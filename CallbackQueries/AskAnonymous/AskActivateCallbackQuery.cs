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
    public class AskActivateCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
			return callbackQuery.Data.Equals("&ask_anon_activate&");
		}

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient)
		{
            long chatId = callbackQuery.Message.Chat.Id;
			int userId = callbackQuery.From.Id;

            Logger.Log.Debug($"Initiated &ask_anon_activate& from #chatId={chatId} by #userId={userId} with #data={callbackQuery.Data}");

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
                        Logger.Log.Debug("&ask_anon_activate& Record not exists in db.MyChatMembers");
                        Logger.Log.Debug($"&ask_anon_activate& SendTextMessage #chatId={chatId} #msg={msg}");
                        await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
                        return;
                    }

                    var record = db.AskAnonymousParticipants
                            .OrderBy(r => r.ChatId)
                            .Where(r => r.ChatId.Equals(chatId))
                            .Where(r => r.UserId.Equals(userId))
                            .FirstOrDefault();

                    if (record == null)
                    {
                        Logger.Log.Debug($"&ask_anon_activate& Add record: (#chatId={chatId} #userId={userId}) to db.AskAnonymousParticipants");
                        db.Add(new Participant { ChatId = chatId, UserId = userId });
                        await db.SaveChangesAsync();
                        msg += "Пользователь " + mention + " подписался на анонимные вопросы!";
                    }
                    else
                    {
                        Logger.Log.Debug($"&ask_anon_activate& Record exists: (#id={record.Id} #chatId={chatId} #userId={record.UserId}) in db.AskAnonymousParticipants");
                        msg += "Пользователь " + mention + " уже подписан на анонимные вопросы!";
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log.Error("&ask_anon_activate& Error while processing database", ex);
            }

            try
            {
                Logger.Log.Debug($"&ask_anon_activate& SendTextMessage #chatId={chatId} #msg={msg}");
                await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("&ask_anon_activate& Error while SendTextMessage", ex);
            }
        }
    }
}
