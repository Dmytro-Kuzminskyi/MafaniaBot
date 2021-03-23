using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using StackExchange.Redis;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
    public class AskSelectUserCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message.Chat.Type != ChatType.Private)
                return false;

            return callbackQuery.Data.StartsWith("ask_select_user&");
        }

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            try
            {
                List<int> userlist = null;
                HashEntry[] record = null;
                string data = callbackQuery.Data;
                long chatId = callbackQuery.Message.Chat.Id;
                int userId = callbackQuery.From.Id;
                int messageId = callbackQuery.Message.MessageId;
                string recipientData = data.Split('&')[1];
                long toChatId = long.Parse(recipientData.Split(':')[0]);
                int toUserId = int.Parse(recipientData.Split(':')[1]);
                string chatTitle = null;
                string msg = null;

                Logger.Log.Debug($"Initiated AskSelectUserCallback by #userId={callbackQuery.Message.Chat.Id} with #data={callbackQuery.Data}");

                try
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    var key = new RedisKey($"PendingQuestion:{userId}");

                    record = await db.HashGetAllAsync(key);
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"AskSelectUserCallback Error while processing Redis.PendingQuestions", ex);
                }

                if (record == null)
                {
                    msg = "Бот удален из чата, невозможно задать вопрос!";

                    Logger.Log.Debug($"AskSelectUserCallback DeleteMessage #chatId={chatId} #messageId={messageId}");

                    await botClient.DeleteMessageAsync(chatId, messageId);

                    Logger.Log.Debug($"AskSelectUserCallback SendTextMessage #chatId={chatId} #msg={msg}");

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg);

                    return;
                }

                ChatMember member = null;

                try
                {
                    member = await botClient.GetChatMemberAsync(toChatId, toUserId);

                    Logger.Log.Debug($"AskSelectUserCallback #member={toUserId} of #chatId={chatId}");
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"AskSelectUserCallback ChatMember not exists", ex);
                }

                if (member != null)
                {
                    if (member.Status == ChatMemberStatus.Creator || member.Status == ChatMemberStatus.Administrator || member.Status == ChatMemberStatus.Member)
                    {
                        string firstname = member.User.FirstName;
                        string lastname = member.User.LastName;

                        string username = lastname != null ? firstname + " " + lastname : firstname;

                        string mention = $"<a href=\"tg://user?id={toUserId}\">" + Helper.ConvertTextToHtmlParseMode(username) + "</a>";

                        try
                        {
                            IDatabaseAsync db = redis.GetDatabase();

                            var key = new RedisKey($"PendingQuestion:{userId}");
                            var field = new RedisValue("ChatTitle");

                            chatTitle = await db.HashGetAsync(key, field);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error($"AskSelectUserCallback Error while processing db.PendingQuestion:{userId}", ex);
                        }

                        msg += $"Напишите анонимный вопрос для {mention} из чата <b>{Helper.ConvertTextToHtmlParseMode(chatTitle)}</b>";

                        var buttonCancel = InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&");
                        var keyboardCancel = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { buttonCancel } });

                        Logger.Log.Debug($"AskSelectUserCallback EditMessageText #chatId={chatId} #msg={msg}");

                        await botClient.EditMessageTextAsync(chatId, messageId, msg, ParseMode.Html, replyMarkup: keyboardCancel);

                        try
                        {
                            IDatabaseAsync db = redis.GetDatabase();

                            var key = new RedisKey($"PendingQuestion:{userId}");
                            var entry = new HashEntry("ToUserId", toUserId);

                            await db.HashSetAsync(key, new[] { entry });
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error($"AskSelectUserCallback Error while processing db.PendingQuestion:{userId}", ex);
                        }
                    }
                    return;
                }

                msg = "Этот пользователь покинул чат!";

                Logger.Log.Debug($"AskSelectUserCallback AnswerCallbackQuery #chatId={chatId} #msg={msg}");

                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg);

                try
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    var key = new RedisKey($"AskParticipants:{toChatId}");

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
                    Logger.Log.Error($"AskSelectChatCallback Error while processing Redis.AskParticipants:{toChatId}", ex);
                }

                userlist.Remove(userId);

                if (userlist.Count == 0)
                {
                    msg = "Некому задать анонимный вопрос, подожди пока кто-то подпишется!";

                    Logger.Log.Debug($"AskSelectChatCallback DeleteMessage #chatId={chatId} #messageId={messageId}");

                    await botClient.DeleteMessageAsync(chatId, messageId);

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg);

                    return;
                }

                int[] userIds = userlist.ToArray();

                var tasks = userIds.Where(id => id != userId).Select(id => botClient.GetChatMemberAsync(toChatId, id));

                ChatMember[] chatMembers = await Task.WhenAll(tasks);

                var keyboardData = new List<KeyValuePair<string, string>>();

                foreach (var chatMember in chatMembers)
                {
                    if (chatMember.Status == ChatMemberStatus.Creator || chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Member)
                    {
                        string firstname = chatMember.User.FirstName;
                        string lastname = chatMember.User.LastName;
                        toUserId = chatMember.User.Id;
                        string username = lastname != null ? firstname + " " + lastname : firstname;

                        keyboardData.Add(new KeyValuePair<string, string>(username, $"ask_select_user&{toChatId}:{toUserId}"));
                    }
                }

                var keyboard = Helper.CreateInlineKeyboard(keyboardData, 2, "CallbackData").InlineKeyboard.ToList();

                var cancelBtn = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&") };

                keyboard.Add(cancelBtn);

                try
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    var key = new RedisKey($"PendingQuestion:{userId}");
                    var field = new RedisValue("ChatTitle");

                    chatTitle = await db.HashGetAsync(key, field);
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"AskSelectUserCallback Error while processing db.PendingQuestion:{userId}", ex);
                }

                msg = $"Выберите участника группы <b>{Helper.ConvertTextToHtmlParseMode(chatTitle)}</b>, которому вы желаете задать анонимный вопрос:";

                Logger.Log.Debug($"AskSelectChatCallback SendTextMessage #chatId={chatId} #msg={msg}");

                await botClient.EditMessageTextAsync(chatId, messageId, msg, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(keyboard));

                try
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    var key = new RedisKey($"PendingQuestion:{userId}");
                    var entry0 = new HashEntry("ChatId", toChatId.ToString());
                    var entry1 = new HashEntry("ChatTitle", chatTitle);

                    await db.HashSetAsync(key, new[] { entry0, entry1 });
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"AskSelectChatCallback Error while processing Redis.PendingQuestions", ex);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("AskSelectUserCallback ---", ex);
            }
        }
    }
}