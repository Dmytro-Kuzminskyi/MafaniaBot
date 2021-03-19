using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
	public class AskInitiateCallbackQuery : Entity<CallbackQuery>
	{
		public override bool Contains(CallbackQuery callbackQuery)
		{
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
				"[" + firstname + " " + lastname + "](tg://user?id=" + userId + ")" :
				"[" + firstname + "](tg://user?id=" + userId + ")";
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
                        msg += mention + ", сначала зарегистрируйся!";
                        try
                        {
                            Logger.Log.Debug($"&ask_anon_question& SendTextMessage #chatId={chatId} #msg={msg}");
                            await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error("&ask_anon_question& Error while SendTextMessage", ex);
                        }
                        return;
                    }
                    else
                    {
                        //TODO Check If bot is banned
                    }

                    var recordPendingAnswer = db.PendingAnonymousAnswers
                            .OrderBy(r => r.FromUserId)
                            .Where(r => r.FromUserId.Equals(userId))
                            .FirstOrDefault();

                    if (recordPendingAnswer != null)
                    {
                        msg += "Сначала ответь на вопрос!";

                        try
                        {
                            Logger.Log.Debug($"&ask_anon_question& SendTextMessage #chatId={userId} #msg={msg}");
                            await botClient.SendTextMessageAsync(userId, msg);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error("&ask_anon_question& Error while SendTextMessage", ex);
                        }
                        return;
                    }

                    var recordPendingQuestion = db.PendingAnonymousQuestions
                            .OrderBy(r => r.FromUserId)
                            .Where(r => r.FromUserId.Equals(userId))
                            .FirstOrDefault();

                    if (recordPendingQuestion != null)
                    {
                        msg += "Сначала закончи с предыдущим вопросом!";

                        try
                        {
                            Logger.Log.Debug($"&ask_anon_question& SendTextMessage #chatId={userId} #msg={msg}");
                            await botClient.SendTextMessageAsync(userId, msg);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error("&ask_anon_question& Error while SendTextMessage", ex);
                        }
                        return;
                    }

                    var recordset = db.AskAnonymousParticipants
                            .OrderBy(r => r.ChatId)
                            .Where(r => !r.UserId.Equals(userId))
                            .Select(r => r.UserId);

                    List<int> userlist = recordset.ToList();

                    if (userlist.Count == 0)
                    {
                        msg += "Некому задать анонимный вопрос, подожди пока кто-то подпишется!";

                        try
                        {
                            Logger.Log.Debug($"&ask_anon_question& SendTextMessage #chatId={userId} #msg={msg}");
                            await botClient.SendTextMessageAsync(userId, msg);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error("&ask_anon_question& Error while SendTextMessage", ex);
                        }
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

                            var keyboard = Helpers.GetInlineKeyboard(keyboardData, 3, "CallbackData");

                            try
                            {
                                Logger.Log.Debug($"&ask_anon_question& Add record: (#chatId={chatId} #userId={userId}) to db.PendingAnonymousQuestions");
                                db.Add(new PendingQuestion { ChatId = chatId, FromUserId = userId });
                                await db.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error("&ask_anon_question& Error while processing database", ex);
                            }

                            msg += "Выбери кому ты хочешь задать анонимный вопрос:";

                            try
                            {
                                Logger.Log.Debug($"&ask_anon_question& SendTextMessage #chatId={userId} #msg={msg}");
                                await botClient.SendTextMessageAsync(userId, msg, replyMarkup: keyboard);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error("&ask_anon_question& Error while SendTextMessage", ex);
                            }
                        }
                        else
                        {
                            msg += "Некому задать анонимный вопрос, подожди пока кто-то подпишется!";

                            try
                            {
                                Logger.Log.Debug($"&ask_anon_question& SendTextMessage #chatId={userId} #msg={msg}");
                                await botClient.SendTextMessageAsync(userId, msg);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error("&ask_anon_question& Error while SendTextMessage", ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("&ask_anon_question& Error while processing callbackQuery", ex);
            }
		}
	}
}