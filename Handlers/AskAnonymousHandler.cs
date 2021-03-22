using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
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

        public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer cache)
        {
            try
            {
                bool isBotBlocked = false;
                long chatId = message.Chat.Id;
                int userId = message.From.Id;

                Logger.Log.Debug($"AskAnonymous HANDLER triggered in #chatId={chatId} #userId={userId}");

                string msg = null;

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
                                Logger.Log.Debug($"AskAnonymous HANDLER Add record: (#chatId={recordPendingQuestion.ChatId} #fromUserId={recordPendingQuestion.FromUserId}#toUserId={recordPendingQuestion.ToUserId}#text={question}) to db.AnonymousQuestions");

                                db.Add(new Question
                                {
                                    ChatId = recordPendingQuestion.ChatId,
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

                            msg = "Новый анонимный вопрос для " + mention + "!";

                            var buttonShow = InlineKeyboardButton.WithCallbackData("Посмотреть",
                                "show&" + recordQuestion.ToUserId + ":" + recordQuestion.Id);
                            var buttonAnswer = InlineKeyboardButton.WithCallbackData("Ответить",
                                "answer&" + recordQuestion.FromUserId + ":" + recordQuestion.ToUserId + ":" + recordQuestion.Id);

                            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[] { buttonShow, buttonAnswer });

                            Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={chatId} #msg={msg}");

                            await botClient.SendTextMessageAsync(chatId, "Вопрос успешно отправлен!");

                            Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={recordQuestion.ChatId} #msg={msg}");

                            await botClient.SendTextMessageAsync(recordQuestion.ChatId, msg, ParseMode.Html, replyMarkup: keyboard);

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
                        return;
                    }

                    var recordPendingAnswer = db.PendingAnonymousAnswers
                        .OrderBy(r => r.FromUserId)
                        .Where(r => r.FromUserId.Equals(userId))
                        .FirstOrDefault();

                    if (recordPendingAnswer != null)
                    {
                        string mention = $"<a href=\"tg://user?id={recordPendingAnswer.FromUserId}\">" + Helper.ConvertTextToHtmlParseMode(recordPendingAnswer.FromUserName) + "</a>";

                        msg += "Ответ пользователя " + mention + " на ваш вопрос:" +
                            "\n" + Helper.ConvertTextToHtmlParseMode(message.Text);
                        try
                        {
                            Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={recordPendingAnswer.ToUserId} #msg={msg}");

                            await botClient.SendTextMessageAsync(recordPendingAnswer.ToUserId, msg, ParseMode.Html);
                        }
                        catch (ApiRequestException apiEx)
                        {
                            if (apiEx.ErrorCode == 403)
                            {
                                Logger.Log.Warn($"AskAnonymous HANDLER Forbidden: bot was blocked by the user - #userId={recordPendingAnswer.ToUserId}");

                                isBotBlocked = true;

                                try
                                {
                                    var record = db.MyChatMembers
                                        .OrderBy(r => r.UserId)
                                        .Where(r => r.UserId.Equals(recordPendingAnswer.ToUserId))
                                        .FirstOrDefault();

                                    if (record != null)
                                    {
                                        db.Remove(record);
                                        await db.SaveChangesAsync();
                                    }
                                }
                                catch (Exception dbEx)
                                {
                                    Logger.Log.Error("AskAnonymous HANDLER Error while processing db.MyChatMembers", dbEx);
                                }

                                msg = "Пользователь заблокировал бота!";

                                Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={chatId} #msg={msg}");

                                await botClient.SendTextMessageAsync(chatId, msg);
                            }
                        }

                        if (!isBotBlocked)
                        {
                            Logger.Log.Debug($"AskAnonymous HANDLER SendTextMessage #chatId={chatId} #msg=Ответ успешно отправлен!");

                            await botClient.SendTextMessageAsync(chatId, "Ответ успешно отправлен!");
                        }

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
