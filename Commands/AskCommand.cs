using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using StackExchange.Redis;

namespace MafaniaBot.Commands
{
    public class AskCommand : Command
    {
        public override string Pattern { get; }

        public override string Description { get; }

        public AskCommand()
        {
            Pattern = @"/ask";
            Description = "Задать анонимный вопрос";
        }

        public override bool Contains(Message message)
        {
            return (message.Text.Equals(Pattern) || message.Text.Equals(Pattern + Startup.BOT_USERNAME)) && !message.From.IsBot;
        }

        public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            try
            {
                List<long> groupList = null;
                long chatId = message.Chat.Id;
                int userId = message.From.Id;
                int messageId = message.MessageId;
                bool result = false;
                string msg = null;

                if (message.Chat.Type != ChatType.Private)
                {
                    msg = $"Эта команда доступна только в <a href=\"{Startup.BOT_URL}\">личных сообщениях</a>!";

                    Logger.Log.Debug($"/ASK SendTextMessage #chatId={chatId} #msg={msg}");

                    await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html, disableWebPagePreview: true, replyToMessageId: messageId);

                    return;
                }

                try
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    var key = new RedisKey("MyChatMembers");
                    var value = new RedisValue(userId.ToString());

                    result = await db.SetContainsAsync(key, value);
                }
                catch (Exception ex)
                {
                    Logger.Log.Error("/ASK Error while processing Redis.MyChatMembers", ex);
                }

                if (!result)
                {
                    msg = "Cначала зарегистрируйтесь!";

                    Logger.Log.Debug($"/ASK SendTextMessage #chatId={chatId} #msg={msg}");

                    await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html);

                    return;
                }

                //TODO Check Pending Answer

                //TODO Check Pending Question

                try
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    var key = new RedisKey("MyGroups");

                    RedisValue[] recordset = await db.SetMembersAsync(key);

                    groupList = new List<long>();

                    for (int i = 0; i < recordset.Length; i++)
                    {
                        string v = recordset[i].ToString();
                        groupList.Add(long.Parse(v));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"/ASK Error while processing Redis.MyGroups", ex);
                }

                var chatsAvailable = new List<long>();

                foreach (var groupId in groupList)
                {
                    try
                    {
                        ChatMember member = await botClient.GetChatMemberAsync(groupId, userId);
                        if (member.Status == ChatMemberStatus.Creator || member.Status == ChatMemberStatus.Administrator || member.Status == ChatMemberStatus.Member)
                        {
                            chatsAvailable.Add(groupId);
                        }
                    }
                    catch (ApiRequestException ex)
                    {
                        Logger.Log.Warn($"/ASK Not found #userId={userId} in #chatId={groupId}", ex);
                    }
                }

                var tasks = chatsAvailable.Select(chatId => botClient.GetChatAsync(chatId));

                Chat[] chats = await Task.WhenAll(tasks);

                if (chats.Length == 0)
                {
                    msg = "Нет подходящих чатов, где можно задать анонимный вопрос!\n" +
                        "Вы можете добавить бота в группу";

                    var buttonAdd = InlineKeyboardButton.WithUrl("Добавить в группу", Startup.BOT_URL + "?startgroup=1");

                    var keyboardReg = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { buttonAdd } });

                    Logger.Log.Debug($"/ASK SendTextMessage #chatId={chatId} #msg={msg}");

                    await botClient.SendTextMessageAsync(chatId, msg, replyMarkup: keyboardReg);

                    return;
                }

                var keyboardData = new List<KeyValuePair<string, string>>();

                foreach(var chat in chats)
                {
                    keyboardData.Add(new KeyValuePair<string, string>(chat.Title, "ask_select_chat&" + chat.Title + ":" + chat.Id.ToString()));
                }

                var keyboard = Helper.CreateInlineKeyboard(keyboardData, 1, "CallbackData").InlineKeyboard.ToList();

                var cancelBtn = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&") };

                keyboard.Add(cancelBtn);

                msg = "Выберите чат, участникам которого вы желаете задать анонимный вопрос:";

                Logger.Log.Debug($"/ASK SendTextMessage #chatId={chatId} #msg={msg}");

                await botClient.SendTextMessageAsync(chatId, msg, replyMarkup: new InlineKeyboardMarkup(keyboard));
            }
            catch (Exception ex)
            {
                Logger.Log.Error("/START ---", ex);
            }
        }
    }
}