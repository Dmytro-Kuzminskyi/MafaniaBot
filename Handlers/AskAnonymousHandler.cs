using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using StackExchange.Redis;

namespace MafaniaBot.Handlers
{
    public class AskAnonymousHandler : Entity<Message>
    {
        public override bool Contains(Message message)
        {
            if (message.Chat.Type != ChatType.Private)
                return false;

            return !message.From.IsBot;
        }

        public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {

            long chatId = message.Chat.Id;
            int userId = message.From.Id;

            bool isBotBlocked = false;
            int toUserId;

            Logger.Log.Debug($"AskAnonymous HANDLER triggered in #chatId={chatId} #userId={userId}");

            try
            {
                IDatabaseAsync db = redis.GetDatabase();
                HashEntry[] record = await db.HashGetAllAsync(new RedisKey($"PendingQuestion:{userId}"));

                if (record != null)
                {
                    string msg = 
                            "Новый анонимный вопрос:\n" + 
                            Helper.ConvertTextToHtmlParseMode(message.Text);
                    var result = record.ToDictionary();
                    toUserId = int.Parse(result["ToUserId"].ToString());
                    var buttonAnswer = InlineKeyboardButton.WithCallbackData("Ответить", $"answer&{userId}");
                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[] { buttonAnswer });
                    Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={toUserId} #msg={msg}");
                    await botClient.SendTextMessageAsync(toUserId, msg, ParseMode.Html, replyMarkup: keyboard);
                    //TODO Check is user banned the bot
                    Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={toUserId} #msg=Вопрос успешно отправлен!");
                    await botClient.SendTextMessageAsync(chatId, "Вопрос успешно отправлен!");
                    await db.KeyDeleteAsync(new RedisKey($"PendingQuestion:{userId}"));
                }
                return;
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"AskAnonymous HANDLER Error while processing Redis.PendingQuestion:{userId}", ex);
            }

            try
            {
                IDatabaseAsync db = redis.GetDatabase();
                var key = new RedisKey($"PendingAnswer:{userId}");
                var value = await db.StringGetAsync(key);

                if (!value.IsNull)
                {
                    toUserId = int.Parse(value.ToString());
                    var msg = $"Ответ пользователя  на ваш вопрос\n" +
                        Helper.ConvertTextToHtmlParseMode(message.Text);

                    try
                    {
                        Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={toUserId} #msg={msg}");
                        await botClient.SendTextMessageAsync(toUserId, msg);
                    }
                    catch (ApiRequestException apiEx)
                    {
                        if (apiEx.ErrorCode == 403)
                        {
                            Logger.Log.Warn($"AskAnonymous HANDLER Forbidden: bot was blocked by the user - #userId={toUserId}");
                            isBotBlocked = true;
                            var k = new RedisKey("MyChatMembers");
                            var v = new RedisValue(toUserId.ToString());
                            await db.SetRemoveAsync(k, v);
                            msg = "Сообщение не отправлено, пользователь заблокировал бота!";
                            Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={chatId} #msg={msg}");
                            await botClient.SendTextMessageAsync(chatId, msg);
                        }
                    }

                    if (!isBotBlocked)
                    {
                        Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={chatId} #msg=Ответ успешно отправлен!");
                        await botClient.SendTextMessageAsync(chatId, "Ответ успешно отправлен!");
                        await db.KeyDeleteAsync(key);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"AskAnonymous HANDLER Error while processing Redis.PendingAnswer:{userId}", ex);
            }
        }
    }
}