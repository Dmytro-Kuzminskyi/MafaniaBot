using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
	public class AskSelectUserCallbackQuery : Entity<CallbackQuery>
	{
		public override bool Contains(CallbackQuery callbackQuery)
		{
			return callbackQuery.Message.Text.Equals("Выбери кому ты хочешь задать анонимный вопрос:");
		}

		public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient)
		{
			string data = callbackQuery.Data;
			long chatId = long.Parse(data.Split(':')[0]);
			int toUserId = int.Parse(data.Split(':')[1]);

			long currentChatId = callbackQuery.Message.Chat.Id;
			int messageId = callbackQuery.Message.MessageId;
            string msg = null;

            Logger.Log.Debug($"Initiated SelectUserCallback by #userId={currentChatId} with #data={callbackQuery.Data}");

            try
            {
                using (var db = new MafaniaBotDBContext())
                {
                    var record = db.PendingAnonymousQuestions
                        .OrderBy(r => r.FromUserId)
                        .Where(r => r.FromUserId.Equals(callbackQuery.From.Id))
                        .Where(r => r.ChatId.Equals(chatId))
                        .FirstOrDefault();

                    if (record.ToUserId.ToString().Equals("0") && record.ToUserName == null)
                    {
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

                            string mention = "[" + username + "](tg://user?id=" + toUserId + ")";
                            msg += "Напиши анонимный вопрос для: " + mention;

                            record.ToUserId = toUserId;
                            record.ToUserName = username;

                            try
                            {
                                db.Update(record);
                                await db.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error("SelectUserCallback Error while processing database", ex);
                            }

                            try
                            {
                                Logger.Log.Debug($"SelectUserCallback SendTextMessage #chatId={currentChatId} #msg={msg}");
                                await botClient.EditMessageTextAsync(currentChatId, messageId, msg, ParseMode.MarkdownV2);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error("SelectUserCallback Error while SendTextMessage", ex);
                            }
                        }
                        else
                        {
                            msg += "Этот пользователь покинул чат!";

                            try
                            {
                                Logger.Log.Debug($"SelectUserCallback SendTextMessage #chatId={currentChatId} #msg={msg}");
                                await botClient.SendTextMessageAsync(currentChatId, msg);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error("SelectUserCallback Error while SendTextMessage", ex);
                            }

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

                                var keyboard = Helpers.GetInlineKeyboard(keyboardData, 3, "CallbackData");

                                msg = "Выбери кому ты хочешь задать анонимный вопрос:";

                                try
                                {
                                    Logger.Log.Debug($"SelectUserCallback DeleteMessage #chatId={currentChatId} #messageId={messageId}");
                                    await botClient.DeleteMessageAsync(currentChatId, messageId);
                                    Logger.Log.Debug($"SelectUserCallback SendTextMessage #chatId={currentChatId} #msg={msg}");
                                    await botClient.SendTextMessageAsync(currentChatId, msg, replyMarkup: keyboard);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log.Error("SelectUserCallback Error while SendTextMessage", ex);
                                }
                            }                            
                        }
                    }
                    else
                    {
                        try
                        {
                            Logger.Log.Debug($"SelectUserCallback DeleteMessage #chatId={currentChatId} #messageId={messageId}");
                            await botClient.DeleteMessageAsync(currentChatId, messageId);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Error("SelectUserCallback Error while DeleteMessage", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("SelectUserCallback Error while processing database", ex);
            }
		}
	}
}