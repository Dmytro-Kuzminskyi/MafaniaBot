using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using System;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
    public class ShowQuestionCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
            return callbackQuery.Message.Text.StartsWith("Новый анонимный вопрос для") && callbackQuery.Data.StartsWith("show&");
        }

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient)
        {
            string data = callbackQuery.Data.Split('&')[1];
            int recipientId = int.Parse(data.Split(':')[0]);
            int messageId = int.Parse(data.Split(':')[1]);
            string msg = null;

            Logger.Log.Debug($"Initiated show& from #chatId={callbackQuery.Message.Chat.Id} by #userId={callbackQuery.From.Id} with #data={callbackQuery.Data}");

            try
            {
                using (var db = new MafaniaBotDBContext())
                {
                    msg = db.AnonymousQuestions
                        .OrderBy(r => r.Id)
                        .Where(r => r.Id.Equals(messageId))
                        .Where(r => r.ToUserId.Equals(recipientId))
                        .Select(r => r.Text)
                        .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("&show& Error while processing database", ex);
            }

            if (msg != null)
            {
                if (callbackQuery.From.Id.Equals(recipientId))
                {
                    try
                    {
                        Logger.Log.Debug($"show& AnswerCallbackQuery #callbackQueryId={callbackQuery.Id} #msg={msg}");
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error("show& Error while SendTextMessage", ex);
                    }
                }
                else
                {
                    msg = "Этот вопрос не для тебя!";

                    try
                    {
                        Logger.Log.Debug($"show& AnswerCallbackQuery #callbackQueryId={callbackQuery.Id} #msg={msg}");
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg, true);
                    }
                    catch (Exception ex) 
                    {
                        Logger.Log.Error("show& Error while SendTextMessage", ex);
                    }
                }
            }
            else
            {
                msg = "Ошибка получения вопроса!";

                try
                {
                    Logger.Log.Debug($"show& AnswerCallbackQuery #callbackQueryId={callbackQuery.Id} #msg={msg}");
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg, true);
                }
                catch (Exception ex)
                {
                    Logger.Log.Error("show& Error while SendTextMessage", ex);
                }
            }
        }
    }
}
