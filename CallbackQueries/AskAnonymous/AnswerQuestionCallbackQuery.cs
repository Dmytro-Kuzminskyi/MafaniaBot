using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
	public class AnswerQuestionCallbackQuery : Entity<CallbackQuery>
	{
		public override bool Contains(CallbackQuery callbackQuery)
		{
			return callbackQuery.Message.Text.StartsWith("Новый анонимный вопрос для") && 
                callbackQuery.Data.StartsWith("answer&");
		}

		public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient)
		{
			long chatId = callbackQuery.Message.Chat.Id;
			string data = callbackQuery.Data.Split('&')[1];
            int senderId = int.Parse(data.Split(':')[0]);
			int recipientId = int.Parse(data.Split(':')[1]);
			int questionId = int.Parse(data.Split(':')[2]);
            string question = null;
            string msg = null;

            Logger.Log.Debug($"Initiated answer& from #chatId={chatId} by #userId={callbackQuery.From.Id} with #data={callbackQuery.Data}");

            try
            {
                if (callbackQuery.From.Id.Equals(recipientId))
                {
                    using (var db = new MafaniaBotDBContext())
                    {
                        var recordPendingQuestion = db.PendingAnonymousQuestions
                            .OrderBy(r => r.FromUserId)
                            .Where(r => r.FromUserId.Equals(recipientId))
                            .FirstOrDefault();

                        if (recordPendingQuestion != null)
                        {
                            msg += "Сначала закончи с предыдущим вопросом!";

                            Logger.Log.Debug($"answer& SendTextMessage #chatId={recipientId} #msg={msg}");

                            await botClient.SendTextMessageAsync(recipientId, msg);
                            return;
                        }

                        var recordPendingAnswer = db.PendingAnonymousAnswers
                            .OrderBy(r => r.FromUserId)
                            .Where(r => r.FromUserId.Equals(recipientId))
                            .Where(r => r.ChatId.Equals(chatId))
                            .Where(r => r.ToUserId.Equals(senderId))
                            .FirstOrDefault();

                        if (recordPendingAnswer == null)
                        {
                            ChatMember member = await botClient.GetChatMemberAsync(chatId, recipientId);

                            string firstname = member.User.FirstName;
                            string lastname = member.User.LastName;

                            string username = lastname != null ? firstname + " " + lastname : firstname;
                          
                            try
                            {
                                question = db.AnonymousQuestions
                                    .OrderBy(r => r.Id)
                                    .Where(r => r.Id.Equals(questionId))
                                    .Where(r => r.ToUserId.Equals(recipientId))
                                    .Select(r => r.Text)
                                    .LastOrDefault();
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error("answer& Error while processing db.AnonymousQuestions", ex);
                            }

                            msg += "Напиши ответ на анонимный вопрос:" +
                                "\n" + question;

                            Logger.Log.Debug($"answer& SendTextMessage #chatId={recipientId} #msg={msg}");

                            await botClient.SendTextMessageAsync(recipientId, msg);

                            try
                            {
                                Logger.Log.Debug($"answer& Add record: (#chatId={chatId} #fromUserId={recipientId} #fromUserName={username} #toUserId={senderId} #messageId={callbackQuery.Message.MessageId}) to db.PendingAnonymousAnswers");

                                db.Add(new PendingAnswer
                                {
                                    ChatId = chatId,
                                    FromUserId = recipientId,
                                    FromUserName = username,
                                    ToUserId = senderId,
                                    MessageId = callbackQuery.Message.MessageId
                                });

                                await db.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error("answer& Error while processing db.PendingAnonymousAnswers", ex);
                            }
                        }
                        else
                        {
                            msg += "Сначала ответь на вопрос!";

                            Logger.Log.Debug($"answer& SendTextMessage #chatId={recipientId} #msg={msg}");

                            await botClient.SendTextMessageAsync(recipientId, msg);
                            return;
                        }
                    }
                }
                else
                {
                    msg += "Этот вопрос не для тебя!";

                    Logger.Log.Debug($"answer& AnswerCallbackQuery #callbackQueryId={callbackQuery.Id} #msg={msg}");

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg, true);
                }
            } 
            catch (Exception ex)
            {
                Logger.Log.Error("answer& ---", ex);
            }
		}
	}
}
