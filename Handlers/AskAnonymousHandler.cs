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

                                db.Add(new Question
                                {
                                    FromUserId = recordPendingQuestion.FromUserId,
                                    ToUserId = recordPendingQuestion.ToUserId,
                                    Text = question
                                });

                                await db.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error("AskAnonymous HANDLER Error while processing db.AnonymousQuestions", ex);
                            }

                            var recordQuestion = db.AnonymousQuestions
                                .OrderBy(r => r.Id)
                                .Where(r => r.FromUserId.Equals(recordPendingQuestion.FromUserId))
                                .Where(r => r.ToUserId.Equals(recordPendingQuestion.ToUserId))
                                .Where(r => r.Text.Equals(question))
                                .LastOrDefault();                           

                            string mention = $"<a href=\"tg://user?id={recordPendingQuestion.ToUserId}\">" + Helper.ConvertTextToHtmlParseMode(recordPendingQuestion.ToUserName) + "</a>";

                            msg += "Новый анонимный вопрос для " + mention + "!";                        

                            var buttonShow = InlineKeyboardButton.WithCallbackData("Посмотреть",
                                "show&" + recordQuestion.ToUserId + ":" + recordQuestion.Id);
                            var buttonAnswer = InlineKeyboardButton.WithCallbackData("Ответить",
                                "answer&" + recordQuestion.FromUserId + ":" + recordQuestion.ToUserId + ":" + recordQuestion.Id);

                            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[] { buttonShow, buttonAnswer });

                            Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={chatId} #msg={msg}");

                            await botClient.SendTextMessageAsync(chatId, "Вопрос успешно отправлен!");

                            Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={recordPendingQuestion.ChatId} #msg={msg}");

                            await botClient.SendTextMessageAsync(recordPendingQuestion.ChatId, msg, ParseMode.Html, replyMarkup: keyboard);

                            try
                            {
                                Logger.Log.Debug($"AskAnonymous HANDLER Delete record: (#id={recordPendingQuestion.Id} #chatId={recordPendingQuestion.ChatId} #fromUserId={recordPendingQuestion.FromUserId} #toUserId={recordPendingQuestion.ToUserId} #toUserName={recordPendingQuestion.ToUserName}) from db.PendingAnonymousQuestions");

                                db.Remove(recordPendingQuestion);
                                await db.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error("AskAnonymous HANDLER Error while processing db.PendingAnonymousQuestions", ex);
                            }
                        }
                    }

                    var recordPendingAnswer = db.PendingAnonymousAnswers
                        .OrderBy(r => r.FromUserId)
                        .Where(r => r.FromUserId.Equals(userId))
                        .FirstOrDefault();

                    if (recordPendingAnswer != null)
                    {
                        Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={chatId} #msg=Ответ успешно отправлен!");

                        await botClient.SendTextMessageAsync(chatId, "Ответ успешно отправлен!");

                        string mention = $"<a href=\"tg://user?id={recordPendingAnswer.FromUserId}\">" + Helper.ConvertTextToHtmlParseMode(recordPendingAnswer.FromUserName) + "</a>";

                        msg += "Ответ пользователя " + mention + " на ваш вопрос:" +
                            "\n" + Helper.ConvertTextToHtmlParseMode(message.Text);

                        Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={recordPendingAnswer.ToUserId} #msg={msg}");

                        await botClient.SendTextMessageAsync(recordPendingAnswer.ToUserId, msg, ParseMode.Html);

                        Logger.Log.Debug($"AskAnonymous HANDLER DeleteMessage #chatId={recordPendingAnswer.ChatId} #messageId={msg}");

                        await botClient.DeleteMessageAsync(recordPendingAnswer.ChatId, recordPendingAnswer.MessageId);

                        try
                        {
                            var recordQuestion = db.AnonymousQuestions
                                .OrderBy(r => r.Id)
                                .Where(r => r.FromUserId.Equals(recordPendingAnswer.ToUserId))
                                .Where(r => r.ToUserId.Equals(recordPendingAnswer.FromUserId))
                                .LastOrDefault();

                            if (recordQuestion != null)
                            {
                                Logger.Log.Debug($"AskAnonymous HANDLER Delete record: (#id={recordQuestion.Id} #fromUserId={recordQuestion.FromUserId} #toUserId={recordQuestion.ToUserId} #text={recordQuestion.Text}) from db.AnonymousQuestions");

                                db.Remove(recordQuestion);
                                await db.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error("AskAnonymous HANDLER Error while processing db.AnonymousQuestions", ex);
                        }

                        try
                        {
                            Logger.Log.Debug($"AskAnonymous HANDLER Delete record: (#id={recordPendingAnswer.Id} #chatId={recordPendingAnswer.ChatId} #fromUserId={recordPendingAnswer.FromUserId} #fromUserName={recordPendingAnswer.FromUserName} #messageId={recordPendingAnswer.MessageId}) from db.PendingAnonymousAnswers");

                            db.Remove(recordPendingAnswer);
                            await db.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error("AskAnonymous HANDLER Error while processing db.PendingAnonymousAnswers", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("AskAnonymous HANDLER ---", ex);
            }
        }
    }
}
