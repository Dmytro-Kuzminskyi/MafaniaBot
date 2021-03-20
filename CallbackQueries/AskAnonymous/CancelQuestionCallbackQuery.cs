using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace MafaniaBot.CallbackQueries.AskAnonymous
{
    public class CancelQuestionCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message.Chat.Type != ChatType.Private)
                return false;

            return callbackQuery.Data.Equals("&cancel_ask_anon_question&");
        }

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient)
        {
            try
            {
                Logger.Log.Debug($"Initiated &cancel_ask_anon_question& by #userId={callbackQuery.From.Id} with #data={callbackQuery.Data}");

                long chatId = callbackQuery.Message.Chat.Id;
                int userId = callbackQuery.From.Id;
                int messageId = callbackQuery.Message.MessageId;

                using (var db = new MafaniaBotDBContext())
                {
                    PendingQuestion recordPendingQuestion = null;

                    try
                    {
                        recordPendingQuestion = db.PendingAnonymousQuestions
                            .OrderBy(r => r.FromUserId)
                            .Where(r => r.FromUserId.Equals(userId))
                            .FirstOrDefault();

                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error("&cancel_ask_anon_question& Error while processing db.PendingAnonymousQuestions", ex);
                    }

                    if (recordPendingQuestion != null)
                    {
                        try
                        {
                            Logger.Log.Debug($"&cancel_ask_anon_question& Delete record: (#id={recordPendingQuestion.Id} #chatId={recordPendingQuestion.ChatId} #fromUserId={recordPendingQuestion.FromUserId} #toUserId={recordPendingQuestion.ToUserId} #toUserName={recordPendingQuestion.ToUserName}) from db.PendingAnonymousQuestions");

                            db.Remove(recordPendingQuestion);
                            await db.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error("&cancel_ask_anon_question& Error while processing db.PendingAnonymousQuestions", ex);
                        }

                        Logger.Log.Debug($"&cancel_ask_anon_question& DeleteMessage #chatId={chatId} #messageId={messageId}");

                        await botClient.DeleteMessageAsync(chatId, messageId);

                        string msg = "Вы отменили анонимный вопрос!";

                        Logger.Log.Debug($"&cancel_ask_anon_question& SendTextMessage #chatId={chatId} #msg={msg}");

                        await botClient.SendTextMessageAsync(chatId, msg);
                        return;
                    }

                    
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("&cancel_ask_anon_question& ---", ex);
            }
        }
    }
}
