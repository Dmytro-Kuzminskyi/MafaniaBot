using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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

		public override async Task Execute(Message message, ITelegramBotClient botClient)
		{
			long chatId = message.Chat.Id;
			int userId = message.From.Id;

            Logger.Log.Debug($"AskAnonymous HANDLER triggered in #chatId={chatId} #userId={userId}");

            string msg = null;

            try
            {
                using (var db = new MafaniaBotDBContext())
                {
                    var recordPendingQuestion = db.PendingAnonymousQuestions
                        .OrderBy(r => r.FromUserId)
                        .Where(r => r.FromUserId.Equals(userId))
                        .FirstOrDefault();

                    if (recordPendingQuestion != null)
                    {
                        if (!recordPendingQuestion.ToUserId.ToString().Equals("0") && recordPendingQuestion.ToUserName != null)
                        {                       
                            string question = message.Text;

                            try
                            {
                                Logger.Log.Debug($"AskAnonymous HANDLER Add record: (#fromUserId={recordPendingQuestion.FromUserId}#toUserId={recordPendingQuestion.ToUserId}#text={question}) to db.AnonymousQuestions");
                                db.Add(new Question { 
                                    FromUserId = recordPendingQuestion.FromUserId, 
                                    ToUserId = recordPendingQuestion.ToUserId,
                                    Text = question });
                                await db.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error("AskAnonymous HANDLER Error while processing database", ex);
                            }

                            var recordQuestion = db.AnonymousQuestions
                                .OrderBy(r => r.Id)
                                .Where(r => r.FromUserId.Equals(recordPendingQuestion.FromUserId))
                                .Where(r => r.ToUserId.Equals(recordPendingQuestion.ToUserId))
                                .Where(r => r.Text.Equals(question))
                                .FirstOrDefault();

                            msg += "Новый анонимный вопрос для [" + recordPendingQuestion.ToUserName +
                                "](tg://user?id=" + recordPendingQuestion.ToUserId + ")";

                            var buttonShow = InlineKeyboardButton.WithCallbackData("Посмотреть",
                                "show&" + recordQuestion.ToUserId + ":" + recordQuestion.Id);
                            var buttonAnswer = InlineKeyboardButton.WithCallbackData("Ответить",
                                "answer&" + recordQuestion.FromUserId + ":" + recordQuestion.ToUserId + ":" + recordQuestion.Id);

                            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[] { buttonShow, buttonAnswer });
                            try
                            {
                                Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={chatId} #msg={msg}");
                                await botClient.SendTextMessageAsync(chatId, "Вопрос успешно отправлен!");
                                Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={recordPendingQuestion.ChatId} #msg={msg}");
                                await botClient.SendTextMessageAsync(recordPendingQuestion.ChatId, msg, ParseMode.MarkdownV2, replyMarkup: keyboard);

                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error("AskAnonymous HANDLER Error while SendTextMessage", ex);
                            }
                            try
                            {
                                Logger.Log.Debug($"AskAnonymous HANDLER Delete record: (#id={recordPendingQuestion.Id} #chatId={recordPendingQuestion.ChatId} #fromUserId={recordPendingQuestion.FromUserId} #toUserId={recordPendingQuestion.ToUserId} #toUserName={recordPendingQuestion.ToUserName}) from db.PendingAnonymousQuestions");
                                db.Remove(recordPendingQuestion);
                                await db.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error("AskAnonymous HANDLER Error while processing database", ex);
                            }
                        }
                    }

                    var recordPendingAnswer = db.PendingAnonymousAnswers
                        .OrderBy(r => r.FromUserId)
                        .Where(r => r.FromUserId.Equals(userId))
                        .FirstOrDefault();

                    if (recordPendingAnswer != null)
                    {
                        try
                        {
                            Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={chatId} #msg=Ответ успешно отправлен!");
                            await botClient.SendTextMessageAsync(chatId, "Ответ успешно отправлен!");
                        } 
                        catch (Exception ex)
                        {
                            Logger.Log.Error("AskAnonymous HANDLER Error while SendTextMessage", ex);
                        }

                        string mention = "[" + recordPendingAnswer.FromUserName + "](tg://user?id=" + recordPendingAnswer.FromUserId + ")";

                        msg += "Ответ пользователя " + mention + " на ваш вопрос:" +
                            "\n" + message.Text;
                        try
                        {
                            Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={recordPendingAnswer.ToUserId} #msg={msg}");
                            await botClient.SendTextMessageAsync(recordPendingAnswer.ToUserId, msg, ParseMode.MarkdownV2);
                            Logger.Log.Debug($"AskAnonymous HANDLER DeleteMessage #chatId={recordPendingAnswer.ChatId} #messageId={msg}");
                            await botClient.DeleteMessageAsync(recordPendingAnswer.ChatId, recordPendingAnswer.MessageId);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error("AskAnonymous HANDLER Error while SendTextMessage", ex);
                        }

                        try
                        {
                            Logger.Log.Debug($"AskAnonymous HANDLER Delete record: (#id={recordPendingAnswer.Id} #chatId={recordPendingAnswer.ChatId} #fromUserId={recordPendingAnswer.FromUserId} #fromUserName={recordPendingAnswer.FromUserName} #messageId={recordPendingAnswer.MessageId}");
                            db.Remove(recordPendingAnswer);
                            await db.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error("AskAnonymous HANDLER Error while processing database", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("AskAnonymous HANDLER Error while processing database", ex);
            }
		}
	}
}
