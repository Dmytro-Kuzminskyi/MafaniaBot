using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StackExchange.Redis;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
    public class AskCancelCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message.Chat.Type != ChatType.Private)
                return false;

            return callbackQuery.Data.Equals("ask_cancel&");
        }

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            try
            {
                Logger.Log.Debug($"Initiated ask_cancel& by #userId={callbackQuery.From.Id} with #data={callbackQuery.Data}");

                long chatId = callbackQuery.Message.Chat.Id;
                int userId = callbackQuery.From.Id;
                int messageId = callbackQuery.Message.MessageId;

                try
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    var key = new RedisKey($"PendingQuestion:{userId}");

                    await db.KeyDeleteAsync(key);
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"ask_cancel& Error while processing Redis.PendingQuestion:{userId}", ex);
                }

                Logger.Log.Debug($"ask_cancel& DeleteMessage #chatId={chatId} #messageId={messageId}");

                await botClient.DeleteMessageAsync(chatId, messageId);

                string msg = "Вы отменили анонимный вопрос!";

                Logger.Log.Debug($"ask_cancel& SendTextMessage #chatId={chatId} #msg={msg}");

                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("ask_cancel& ---", ex);
            }
        }
    }
}