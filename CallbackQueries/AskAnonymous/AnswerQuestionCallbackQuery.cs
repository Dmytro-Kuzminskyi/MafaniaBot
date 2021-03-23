using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using StackExchange.Redis;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
    public class AnswerQuestionCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message.Chat.Type != ChatType.Private)
                return false;

            return callbackQuery.Data.StartsWith("answer&");
        }

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            HashEntry[] record = null;
            long chatId = callbackQuery.Message.Chat.Id;
            int userId = callbackQuery.From.Id;
            int messageId = callbackQuery.Message.MessageId;

            int fromUserId = int.Parse(callbackQuery.Data.Split('&')[1]);

            string msg = null;

            Logger.Log.Debug($"Initiated answer& from #chatId={chatId} by #userId={callbackQuery.From.Id} with #data={callbackQuery.Data}");

            try
            {
                try
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    var key = new RedisKey($"PendingQuestion:{userId}");

                    record = await db.HashGetAllAsync(key);
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"answer& Error while processing Redis.PendingQuestion:{userId}", ex);
                }

                if (record.Length != 0)
                {
                    msg = "Сначала закончите с предыдущим вопросом!";

                    Logger.Log.Debug($"answer& SendTextMessage #chatId={chatId} #msg={msg}");

                    await botClient.SendTextMessageAsync(chatId, msg);

                    return;
                }

                try
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    var key = new RedisKey($"PendingAnswer:{userId}");

                    record = await db.HashGetAllAsync(key);
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"answer& Error while processing Redis.PendingAnswer:{userId}", ex);
                }

                if (record.Length != 0)
                {
                    msg = "Сначала ответьте на вопрос!";

                    Logger.Log.Debug($"answer& SendTextMessage #chatId={chatId} #msg={msg}");

                    await botClient.SendTextMessageAsync(chatId, msg);

                    return;
                }

                msg = "Напишите свой ответ на вопрос";

                var buttonCancel = InlineKeyboardButton.WithCallbackData("Отмена", "answer_cancel&");
                var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { buttonCancel } });

                Logger.Log.Debug($"answer& SendTextMessage #chatId={fromUserId} #msg={msg}");

                await botClient.EditMessageTextAsync(userId, messageId, msg, replyMarkup: keyboard);

                try
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    var key = new RedisKey($"PendingAnswer:{userId}");
                    var value = new RedisValue(fromUserId.ToString());

                    await db.StringSetAsync(key, value);
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"answer& Error while processing Redis.PendingAnswer:{userId}", ex);
                }

            }
            catch (Exception ex)
            {
                Logger.Log.Error("answer& ---", ex);
            }
        }
    }
}