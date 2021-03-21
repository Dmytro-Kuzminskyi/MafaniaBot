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
        public override string Pattern { get; }

        public override string Description { get; }

        public StartCommand()
        {
            Pattern = @"/start";
            Description = "";
        }

        public override bool Contains(Message message)
        {
            return message.Text.StartsWith(Pattern) && !message.From.IsBot;
        }

        public override async Task Execute(Message message, ITelegramBotClient botClient)
        {
            try
            {
                long chatId = message.Chat.Id;
                int userId = message.From.Id;
                string firstname = message.From.FirstName;
                string lastname = message.From.LastName;

                string mention = lastname != null ?
                    $"<a href=\"tg://user?id={userId}\">" + Helper.ConvertTextToHtmlParseMode(firstname) + " " + Helper.ConvertTextToHtmlParseMode(lastname) + "</a>" :
                    $"<a href=\"tg://user?id={userId}\">" + Helper.ConvertTextToHtmlParseMode(firstname) + "</a>";

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
                                Logger.Log.Debug($"/START Add record: (#userId={userId}) to db.MyChatMembers");

                                db.Add(new MyChatMember { UserId = userId });
                                await db.SaveChangesAsync();
                            }
                            else
                            {
                                Logger.Log.Debug($"/START Record exists: (#id={record.Id} #userId={record.UserId}) in db.MyChatMembers");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error("/START Error while processing db.MyChatMembers", ex);
                    }
                }

                Logger.Log.Debug($"/START SendTextMessage #chatId={chatId} #msg={msg}");

                await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("/START ---", ex);
            }
        }
    }
}