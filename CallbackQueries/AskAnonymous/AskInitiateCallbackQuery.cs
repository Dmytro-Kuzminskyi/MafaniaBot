using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
	public class AskInitiateCallbackQuery : Entity<CallbackQuery>
	{
		public override bool Contains(CallbackQuery callbackQuery)
		{
            if (callbackQuery.Message.Chat.Type == ChatType.Channel || callbackQuery.Message.Chat.Type == ChatType.Private)
                return false;

            return callbackQuery.Data.Equals("&ask_anon_question&");
		}

		public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient)
		{
			long chatId = callbackQuery.Message.Chat.Id;
			int userId = callbackQuery.From.Id;

            Logger.Log.Debug($"Initiated &ask_anon_question& from #chatId={chatId} by #userId={userId} with #data={callbackQuery.Data}");

            string firstname = callbackQuery.From.FirstName;
			string lastname = callbackQuery.From.LastName;
			string msg = null;

            string mention = lastname != null ?
                $"<a href=\"tg://user?id={userId}\">" + Helper.ConvertTextToHtmlParseMode(firstname) + " " + Helper.ConvertTextToHtmlParseMode(lastname) + "</a>" :
                $"<a href=\"tg://user?id={userId}\">" + Helper.ConvertTextToHtmlParseMode(firstname) + "</a>";

            try
            {
                using (var db = new MafaniaBotDBContext())
                {
                    var recordReg = db.MyChatMembers
                        .OrderBy(r => r.UserId)
                        .Where(r => r.UserId.Equals(userId))
                        .FirstOrDefault();

                    if (recordReg == null)
                    {
                        msg += mention + ", сначала зарегистрируйтесь!";

                        Logger.Log.Debug($"&ask_anon_question& SendTextMessage #chatId={chatId} #msg={msg}");

                        await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html);
                        return;
                    }

                    var recordPendingAnswer = db.PendingAnonymousAnswers
                            .OrderBy(r => r.FromUserId)
                            .Where(r => r.FromUserId.Equals(userId))
                            .FirstOrDefault();

                    if (recordPendingAnswer != null)
                    {
                        msg += "Сначала ответьте на вопрос!";

                        Logger.Log.Debug($"&ask_anon_question& SendTextMessage #chatId={userId} #msg={msg}");

                        await botClient.SendTextMessageAsync(userId, msg);
                        return;
                    }

                    var recordPendingQuestion = db.PendingAnonymousQuestions
                            .OrderBy(r => r.FromUserId)
                            .Where(r => r.FromUserId.Equals(userId))
                            .FirstOrDefault();

                    if (recordPendingQuestion != null)
                    {
                        msg += "Сначала закончите с предыдущим вопросом!";

                        Logger.Log.Debug($"&ask_anon_question& SendTextMessage #chatId={userId} #msg={msg}");

                        await botClient.SendTextMessageAsync(userId, msg);
                        return;
                    }

                    var recordset = db.AskAnonymousParticipants
                            .OrderBy(r => r.ChatId)
                            .Where(r => !r.UserId.Equals(userId))
                            .Select(r => r.UserId);

                    List<int> userlist = recordset.ToList();

                    if (userlist.Count == 0)
                    {
                        msg += "Некому задать анонимный вопрос, подождите пока кто-то подпишется!";

                        Logger.Log.Debug($"&ask_anon_question& SendTextMessage #chatId={userId} #msg={msg}");

                        await botClient.SendTextMessageAsync(userId, msg);
                    }
                    else
                    {
                        List<KeyValuePair<string, string>> keyboardData = new List<KeyValuePair<string, string>>();
                        var tasks = userlist.Select(userId => botClient.GetChatMemberAsync(chatId, userId));
                        ChatMember[] result = await Task.WhenAll(tasks);
                        if (result.Length > 0)
                        {
                            int i = 0;
                            result.ToList().ForEach(member =>
                            {
                                Logger.Log.Debug($"&ask_anon_question& #member[{i++}]={member.User.Id} #status={member.Status}");

                                if (member.Status == ChatMemberStatus.Creator || member.Status == ChatMemberStatus.Administrator || member.Status == ChatMemberStatus.Member)
                                {
                                    string firstname = member.User.FirstName;
                                    string lastname = member.User.LastName;
                                    string mention = lastname != null ? firstname + " " + lastname : firstname;
                                    keyboardData.Add(new KeyValuePair<string, string>(mention, chatId.ToString() + ":" + member.User.Id.ToString()));
                                }
                            });

                            var keyboard = Helper.CreateInlineKeyboard(keyboardData, 3, "CallbackData").InlineKeyboard.ToList();
                                
                            var cancelBtn = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена", "CANCEL") } ;

                            keyboard.Add(cancelBtn);

                            msg += "Выберите кому будем задавать анонимный вопрос:";

                            Logger.Log.Debug($"&ask_anon_question& SendTextMessage #chatId={userId} #msg={msg}");

                            try
                            {
                                await botClient.SendTextMessageAsync(userId, msg, replyMarkup: new InlineKeyboardMarkup(keyboard));
                            }
                            catch (ApiRequestException ex)
                            {
                                Logger.Log.Warn($"&ask_anon_question& Forbidden: bot was blocked by the user - #userId={userId}");
                                if (ex.ErrorCode == 403)
                                {
                                    try
                                    {
                                        var record = db.MyChatMembers
                                            .OrderBy(r => r.UserId)
                                            .Where(r => r.UserId.Equals(userId))
                                            .FirstOrDefault();

                                        if (record != null)
                                        {
                                            db.Remove(record);
                                            await db.SaveChangesAsync();
                                        }
                                    }
                                    catch (Exception dbEx)
                                    {
                                        Logger.Log.Error("&ask_anon_question& Error while processing db.MyChatMembers", dbEx);
                                    }

                                    try
                                    {
                                        var record = db.AskAnonymousParticipants
                                            .OrderBy(r => r.UserId)
                                            .Where(r => r.UserId.Equals(userId))
                                            .Where(r => r.ChatId.Equals(chatId))
                                            .FirstOrDefault();

                                        if (record != null)
                                        {
                                            db.Remove(record);
                                            await db.SaveChangesAsync();
                                        }
                                    }
                                    catch (Exception dbEx)
                                    {
                                        Logger.Log.Error("&ask_anon_question& Error while processing db.AskAnonymousParticipants", dbEx);
                                    }

                                    msg = mention + ", сначала зарегистрируйтесь!";

                                    Logger.Log.Debug($"&ask_anon_question& SendTextMessage #chatId={chatId} #msg={msg}");

                                    await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html);
                                    return;
                                }
                            }

                            try
                            {
                                Logger.Log.Debug($"&ask_anon_question& Add record: (#chatId={chatId} #userId={userId}) to db.PendingAnonymousQuestions");

                                db.Add(new PendingQuestion { ChatId = chatId, FromUserId = userId });
                                await db.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error("&ask_anon_question& Error while processing db.PendingAnonymousQuestions", ex);
                            }
                        }
                        else
                        {
                            msg += "Некому задать анонимный вопрос, подождите пока кто-то подпишется!";

                            Logger.Log.Debug($"&ask_anon_question& SendTextMessage #chatId={userId} #msg={msg}");

                            await botClient.SendTextMessageAsync(userId, msg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("&ask_anon_question& ---", ex);
            }
		}
	}
}