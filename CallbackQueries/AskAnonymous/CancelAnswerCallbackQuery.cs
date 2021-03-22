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
    public class CancelAnswerCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message.Chat.Type != ChatType.Private)
                return false;

            return callbackQuery.Data.Equals("&cancel_answer_anon_question&");
        }

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient, IConnectionMultiplexer cache)
        {
            try
            {
                Logger.Log.Debug($"Initiated &cancel_answer_anon_question& by #userId={callbackQuery.From.Id} with #data={callbackQuery.Data}");

                long chatId = callbackQuery.Message.Chat.Id;
                int userId = callbackQuery.From.Id;
                int messageId = callbackQuery.Message.MessageId;

                using (var db = new MafaniaBotDBContext())
                {
                    PendingAnswer recordPendingAnswer = null;

                    try
                    {
                        recordPendingAnswer = db.PendingAnonymousAnswers
                                .OrderBy(r => r.FromUserId)
                                .Where(r => r.FromUserId.Equals(userId))
                                .FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error("&cancel_answer_anon_question& Error while processing db.PendingAnonymousAnswers", ex);
                    }

                    if (recordPendingAnswer != null)
                    {
                        try
                        {
                            Logger.Log.Debug($"&cancel_answer_anon_question& Delete record: (#id={recordPendingAnswer.Id} #chatId={recordPendingAnswer.ChatId} #fromUserId={recordPendingAnswer.FromUserId} #fromUserName={recordPendingAnswer.FromUserName} #toUserId={recordPendingAnswer.ToUserId} #messageId={recordPendingAnswer.MessageId}) from db.PendingAnonymousAnswers");

                            db.Remove(recordPendingAnswer);
                            await db.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error("&cancel_answer_anon_question& Error while processing db.PendingAnonymousAnswers", ex);
                        }

                        Logger.Log.Debug($"&cancel_answer_anon_question& DeleteMessage #chatId={chatId} #messageId={messageId}");

                        await botClient.DeleteMessageAsync(chatId, messageId);

                        string msg = "Вы отменили ответ на анонимный вопрос!";

                        Logger.Log.Debug($"&cancel_answer_anon_question& SendTextMessage #chatId={chatId} #msg={msg}");

                        await botClient.SendTextMessageAsync(chatId, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("&cancel_answer_anon_question& ---", ex);
            }
        }
    }
}
