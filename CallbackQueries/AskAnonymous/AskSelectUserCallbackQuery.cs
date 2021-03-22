using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using StackExchange.Redis;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
    public class AskSelectUserCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message.Chat.Type != ChatType.Private)
                return false;

            return callbackQuery.Message.Text.Equals("Выберите кому будем задавать анонимный вопрос:");
        }

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient, IConnectionMultiplexer cache)
        {
            string data = callbackQuery.Data;
            int messageId = 0;
            long chatId = 0;
            string msg = null;

            if (data.Equals("CANCEL"))
            {
                try
                {
                    Logger.Log.Debug($"Initiated CancelSelectUserCallback by #userId={callbackQuery.From.Id} with #data={callbackQuery.Data}");
                    chatId = callbackQuery.Message.Chat.Id;
                    messageId = callbackQuery.Message.MessageId;
                    msg = "Вы отменили анонимный вопрос!";

                    Logger.Log.Debug($"CancelSelectUserCallback DeleteMessage #chatId={chatId} #messageId={messageId}");

                    await botClient.DeleteMessageAsync(chatId, messageId);

                    Logger.Log.Debug($"CancelSelectUserCallback SendTextMessage #chatId={chatId} #msg={msg}");

                    await botClient.SendTextMessageAsync(chatId, msg);

                    using (var db = new MafaniaBotDBContext())
                    {
                        try
                        {
                            var recordPendingQuestion = db.PendingAnonymousQuestions
                                .OrderBy(r => r.FromUserId)
                                .Where(r => r.FromUserId.Equals(callbackQuery.From.Id))
                                .FirstOrDefault();

                            if (recordPendingQuestion != null)
                            {
                                Logger.Log.Debug($"CancelSelectUserCallback Delete record: (#id={recordPendingQuestion.Id} #chatId={recordPendingQuestion.ChatId} #fromUserId={recordPendingQuestion.FromUserId} #toUserId={recordPendingQuestion.ToUserId} #toUserName={recordPendingQuestion.ToUserName}) from db.PendingAnonymousQuestions");

                                db.Remove(recordPendingQuestion);
                                await db.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error("CancelSelectUserCallback Error while processing db.PendingAnonymousQuestions", ex);
                        }
                    }

                    return;
                }
                catch (Exception ex)
                {
                    Logger.Log.Error("CancelSelectUserCallback ---", ex);
                }
            }

            try
            {
                Logger.Log.Debug($"Initiated SelectUserCallback by #userId={callbackQuery.Message.Chat.Id} with #data={callbackQuery.Data}");

                chatId = long.Parse(data.Split(':')[0]);
                int toUserId = int.Parse(data.Split(':')[1]);

                long currentChatId = callbackQuery.Message.Chat.Id;
                messageId = callbackQuery.Message.MessageId;

                using (var db = new MafaniaBotDBContext())
                {
                    var record = db.PendingAnonymousQuestions
                        .OrderBy(r => r.FromUserId)
                        .Where(r => r.FromUserId.Equals(callbackQuery.From.Id))
                        .Where(r => r.ChatId.Equals(chatId))
                        .Where(r => r.ToUserId == 0)
                        .Where(r => r.ToUserName == null)
                        .FirstOrDefault();

                    if (record == null)
                    {
                        msg = "Бот удален из чата, невозможно задать вопрос!";

                        Logger.Log.Debug($"SelectUserCallback DeleteMessage #chatId={currentChatId} #messageId={messageId}");

                        await botClient.DeleteMessageAsync(currentChatId, messageId);

                        Logger.Log.Debug($"SelectUserCallback SendTextMessage #chatId={currentChatId} #msg={msg}");

                        await botClient.SendTextMessageAsync(currentChatId, msg);

                        return;
                    }

                    ChatMember member = null;

                    try
                    {
                        member = await botClient.GetChatMemberAsync(chatId, toUserId);

                        Logger.Log.Debug($"SelectUserCallback #member={toUserId} of #chatId={chatId}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error($"SelectUserCallback ChatMember not exists", ex);
                    }

                    if (member.Status == ChatMemberStatus.Creator || member.Status == ChatMemberStatus.Administrator || member.Status == ChatMemberStatus.Member)
                    {
                        string firstname = member.User.FirstName;
                        string lastname = member.User.LastName;

                        string username = lastname != null ? firstname + " " + lastname : firstname;

                        string mention = $"<a href=\"tg://user?id={toUserId}\">" + Helper.ConvertTextToHtmlParseMode(username) + "</a>";

                        msg += "Напишите анонимный вопрос для: " + mention;

                        record.ToUserId = toUserId;
                        record.ToUserName = username;

                        var buttonCancel = InlineKeyboardButton.WithCallbackData("Отмена", "&cancel_ask_anon_question&");
                        var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { buttonCancel } });

                        Logger.Log.Debug($"SelectUserCallback EditMessageText #chatId={currentChatId} #msg={msg}");

                        await botClient.EditMessageTextAsync(currentChatId, messageId, msg, ParseMode.Html, replyMarkup: keyboard);

                        try
                        {
                            db.Update(record);
                            await db.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error("SelectUserCallback Error while processing db.PendingAnonymousQuestions", ex);
                        }
                    }
                    else
                    {
                        msg += "Этот пользователь покинул чат!";

                        Logger.Log.Debug($"SelectUserCallback SendTextMessage #chatId={currentChatId} #msg={msg}");

                        await botClient.SendTextMessageAsync(currentChatId, msg);

                        var recordset = db.AskAnonymousParticipants
                            .OrderBy(r => r.ChatId)
                            .Where(r => !r.UserId.Equals(callbackQuery.From.Id))
                            .Select(r => r.UserId);

                        List<int> userlist = recordset.ToList();

                        List<KeyValuePair<string, string>> keyboardData = new List<KeyValuePair<string, string>>();
                        var tasks = userlist.Select(userId => botClient.GetChatMemberAsync(chatId, userId));
                        ChatMember[] result = await Task.WhenAll(tasks);

                        if (result.Length > 0)
                        {
                            int i = 0;
                            result.ToList().ForEach(member =>
                            {
                                Logger.Log.Debug($"SelectUserCallback #member[{i++}]={member.User.Id} #status={member.Status}");

                                if (member.Status == ChatMemberStatus.Creator || member.Status == ChatMemberStatus.Administrator || member.Status == ChatMemberStatus.Member)
                                {
                                    string firstname = member.User.FirstName;
                                    string lastname = member.User.LastName;
                                    string mention = lastname != null ? firstname + " " + lastname : firstname;
                                    keyboardData.Add(new KeyValuePair<string, string>(mention, chatId.ToString() + ":" + member.User.Id.ToString()));
                                }
                            });

                            var keyboard = Helper.CreateInlineKeyboard(keyboardData, 3, "CallbackData").InlineKeyboard.ToList();

                            var cancelBtn = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена", "CANCEL") };

                            keyboard.Add(cancelBtn);

                            msg = "Выберите кому будем задавать анонимный вопрос:";

                            Logger.Log.Debug($"SelectUserCallback DeleteMessage #chatId={currentChatId} #messageId={messageId}");

                            await botClient.DeleteMessageAsync(currentChatId, messageId);

                            Logger.Log.Debug($"SelectUserCallback SendTextMessage #chatId={currentChatId} #msg={msg}");

                            await botClient.SendTextMessageAsync(currentChatId, msg, replyMarkup: new InlineKeyboardMarkup(keyboard));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("SelectUserCallback ---", ex);
            }
        }
    }
}