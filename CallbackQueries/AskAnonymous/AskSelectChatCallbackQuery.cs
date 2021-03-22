using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using StackExchange.Redis;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
    public class AskSelectChatCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message.Chat.Type != ChatType.Private)
                return false;

            return callbackQuery.Data.StartsWith("ask_select_chat&");
        }

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            try
            {
                List<int> userlist = null;
                string data = callbackQuery.Data;
                long chatId = callbackQuery.Message.Chat.Id;
                int userId = callbackQuery.From.Id;
                int messageId = callbackQuery.Message.MessageId;
                string chatData = data.Split('&')[1];
                string toChatTitle = chatData.Split(':')[0];
                long toChatId = long.Parse(chatData.Split(':')[1]);

                string msg = null;

                Logger.Log.Debug($"Initiated SelectUserCallback by #userId={callbackQuery.Message.Chat.Id} with #data={callbackQuery.Data}");

                try
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    var key = new RedisKey("AskParticipants:" + toChatId.ToString());

                    RedisValue[] recordset = await db.SetMembersAsync(key);

                    userlist = new List<int>();

                    for (int i = 0; i < recordset.Length; i++)
                    {
                        string v = recordset[i].ToString();
                        userlist.Add(int.Parse(v));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"SelectUserCallback Error while processing Redis.AskParticipants:{toChatId}", ex);
                }

                userlist.Remove(userId);

                if (userlist.Count == 0)
                {
                    msg = "Некому задать анонимный вопрос, подожди пока кто-то подпишется!";

                    Logger.Log.Debug($"SelectUserCallback DeleteMessage #chatId={chatId} #messageId={messageId}");

                    await botClient.DeleteMessageAsync(chatId, messageId);

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg);

                    return;
                }

                int[] userIds = userlist.ToArray();

                var tasks = userIds.Where(id => id != userId).Select(id => botClient.GetChatMemberAsync(toChatId, id));

                ChatMember[] chatMembers = await Task.WhenAll(tasks);

                var keyboardData = new List<KeyValuePair<string, string>>();

                foreach (var member in chatMembers)
                {
                    if (member.Status == ChatMemberStatus.Creator || member.Status == ChatMemberStatus.Administrator || member.Status == ChatMemberStatus.Member)
                    {
                        string firstname = member.User.FirstName;
                        string lastname = member.User.LastName;
                        int id = member.User.Id;
                        string username = lastname != null ? firstname + " " + lastname : firstname;

                        keyboardData.Add(new KeyValuePair<string, string>(username, "ask_select_user&" + id.ToString()));
                    }
                }

                var keyboard = Helper.CreateInlineKeyboard(keyboardData, 2, "CallbackData").InlineKeyboard.ToList();

                var cancelBtn = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&") };

                keyboard.Add(cancelBtn);

                msg = $"Выберите участника группы <b>{Helper.ConvertTextToHtmlParseMode(toChatTitle)}</b>, которому вы желаете задать анонимный вопрос:";

                Logger.Log.Debug($"SelectChatCallback SendTextMessage #chatId={chatId} #msg={msg}");

                await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(keyboard));

                try
                {
                    IDatabase db = redis.GetDatabase();

                    var key = new RedisKey("PendingQuestion:" + userId.ToString());
                    var entry0 = new HashEntry("ChatId", toChatId.ToString());
                    var entry1 = new HashEntry("ChatTitle", toChatTitle);

                    await db.HashSetAsync(key, new[] { entry0, entry1 });
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"SelectChatCallback Error while processing Redis.PendingQuestions", ex);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("SelectChatCallback ---", ex);
            }
        }
    }
}