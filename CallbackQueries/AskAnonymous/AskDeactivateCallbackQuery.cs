﻿using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
    public class AskDeactivateCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
            return callbackQuery.Data.Equals("&ask_anon_deactivate&");
		}

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient)
		{           
			long chatId = callbackQuery.Message.Chat.Id;
			int userId = callbackQuery.From.Id;

            Logger.Log.Debug($"Initiated &ask_anon_deactivate& from #chatId={chatId} by #userId={userId} with #data={callbackQuery.Data}");

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
                    var record = db.AskAnonymousParticipants
                            .OrderBy(r => r.ChatId)
                            .Where(r => r.ChatId.Equals(chatId))
                            .Where(r => r.UserId.Equals(userId))
                            .FirstOrDefault();

                    if (record != null)
                    {
                        Logger.Log.Debug($"&ask_anon_deactivate& Delete record: (#id={record.Id} #chatId={record.ChatId} #userId={record.UserId}) from db.AskAnonymousParticipants");
                        db.AskAnonymousParticipants.Remove(record);
                        await db.SaveChangesAsync();
                        msg += "Пользователь " + mention + " отписался от анонимных вопросов!";
                    }
                    else
                    {
                        Logger.Log.Debug("&ask_anon_deactivate& Record not exists in db.AskAnonymousParticipants");
                        msg += "Пользователь " + mention + " не подписан на анонимные вопросы!";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("&ask_anon_deactivate& Error while processing database", ex);
            }

            try
            {
                Logger.Log.Debug($"&ask_anon_deactivate& SendTextMessage #chatId={chatId} #msg={msg}");
                await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("&ask_anon_deactivate& Error while SendTextMessage", ex);
            }
        }
    }
}