using System;
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
    public class ShowQuestionCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message.Chat.Type == ChatType.Channel || callbackQuery.Message.Chat.Type == ChatType.Private)
                return false;

            return callbackQuery.Message.Text.StartsWith("Новый анонимный вопрос для") && callbackQuery.Data.StartsWith("show&");
        }

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient, IConnectionMultiplexer cache)
        {
            string data = callbackQuery.Data.Split('&')[1];
            int recipientId = int.Parse(data.Split(':')[0]);
            int messageId = int.Parse(data.Split(':')[1]);
            string msg = null;

            Logger.Log.Debug($"Initiated show& from #chatId={callbackQuery.Message.Chat.Id} by #userId={callbackQuery.From.Id} with #data={callbackQuery.Data}");
            try
            {
                try
                {
                    using (var db = new MafaniaBotDBContext())
                    {
                        msg = db.AnonymousQuestions
                            .OrderBy(r => r.Id)
                            .Where(r => r.Id.Equals(messageId))
                            .Where(r => r.ToUserId.Equals(recipientId))
                            .Select(r => r.Text)
                            .LastOrDefault();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Error("&show& Error while processing db.AnonymousQuestions", ex);
                }

                if (msg != null)
                {
                    if (callbackQuery.From.Id.Equals(recipientId))
                    {
                        Logger.Log.Debug($"show& AnswerCallbackQuery #callbackQueryId={callbackQuery.Id} #msg={msg}");

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg, true);
                    }
                    else
                    {
                        msg = "Этот вопрос не для вас!";

                        Logger.Log.Debug($"show& AnswerCallbackQuery #callbackQueryId={callbackQuery.Id} #msg={msg}");

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg, true);
                    }
                }
                else
                {
                    msg = "Ошибка получения вопроса!";

                    Logger.Log.Debug($"show& AnswerCallbackQuery #callbackQueryId={callbackQuery.Id} #msg={msg}");

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg, true);

                    Logger.Log.Debug($"show& DeleteMessage #chatId={callbackQuery.Message.Chat.Id} #messageId={callbackQuery.Message.MessageId}");

                    await botClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("show& ---", ex);
            }
        }
    }
}
