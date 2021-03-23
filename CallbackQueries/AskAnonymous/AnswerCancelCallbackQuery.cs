using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StackExchange.Redis;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
    public class AnswerCancelCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message.Chat.Type != ChatType.Private)
                return false;

            return callbackQuery.Data.Equals("answer_cancel&");
        }

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            try
            {
                Logger.Log.Debug($"Initiated answer_cancel& by #userId={callbackQuery.From.Id} with #data={callbackQuery.Data}");

                long chatId = callbackQuery.Message.Chat.Id;
                int userId = callbackQuery.From.Id;
                int messageId = callbackQuery.Message.MessageId;

                IDatabaseAsync db = redis.GetDatabase();
                var key = new RedisKey($"PendingAnswer:{userId}");
                await db.KeyDeleteAsync(key);
                string msg = "Вы отменили ответ на анонимный вопрос!";
                Logger.Log.Debug($"answer_cancel& SendTextMessage #chatId={chatId} #msg={msg}");
                await botClient.SendTextMessageAsync(chatId, msg);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("answer_cancel& ---", ex);
            }
        }
    }
}
