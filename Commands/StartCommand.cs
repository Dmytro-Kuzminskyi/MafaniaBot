﻿using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Commands
{
    public class StartCommand : Command
    {
        public override string pattern => @"/start";

        public override bool Contains(Message message)
        {
            return message.Text.StartsWith(pattern) && !message.From.IsBot;
        }

        public override async Task Execute(Message message, ITelegramBotClient botClient)
        {
            int userId = message.From.Id;
            string firstname = message.From.FirstName;
            string lastname = message.From.LastName;

            string mention = lastname != null ? 
                "[" + firstname + " " + lastname + "](tg://user?id=" + userId + ")" :
                "[" + firstname + "](tg://user?id=" + userId + ")";

            string msg = "Привет, " + mention + "!" + 
                "\n/help - список доступных команд.";

            if (message.Chat.Type == ChatType.Private)
			{
                try
                {
                    using (var db = new MafaniaBotDBContext())
                    {
                        var record = db.MyChatMembers
                            .OrderBy(r => r.UserId)
                            .Where(r => r.UserId.Equals(userId))
                            .FirstOrDefault();

                        if (record == null)
                        {
                            Logger.Log.Debug($"/START db.MyChatMembers.Add #userId={userId}");
                            db.Add(new MyChatMember { UserId = userId });

                            await db.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
				{
                    Logger.Log.Error("Error while processing database", ex);
				}
			}

            Logger.Log.Debug($"/START SendTextMessage #userId={userId} #msg={msg}");
            try
            {
                await botClient.SendTextMessageAsync(userId, msg, ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("/START Error while SendTextMessage", ex);
			}
        }
    }
}