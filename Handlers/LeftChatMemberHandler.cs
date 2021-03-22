using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StackExchange.Redis;

namespace MafaniaBot.Handlers
{
    public class LeftChatMemberHandler : Entity<Message>
    {
        public override bool Contains(Message message)
        {
            if (message.Chat.Type == ChatType.Private || message.Chat.Type == ChatType.Channel)
                return false;

            return (message.LeftChatMember != null) ? true : false;
        }

        public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            try
            {
                long chatId = message.Chat.Id;
                User member = message.LeftChatMember;

                Logger.Log.Debug($"LeftChatMember HANDLER triggered in #chatId={chatId} #memberId={member.Id}");

                string msg = null;

                if (member.Id.Equals(botClient.BotId))
                {
                    try
                    {
                        IDatabaseAsync db = redis.GetDatabase();

                        var key = new RedisKey("MyGroups");
                        var value = new RedisValue(chatId.ToString());

                        await db.SetRemoveAsync(key, value);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error("LeftChatMember HANDLER Error while processing Redis.MyGroups", ex);
                    }
                    /*using (var db = new MafaniaBotDBContext())
                    {
                        var recordsetParticipants = db.AskAnonymousParticipants
                                .OrderBy(r => r.ChatId)
                                .Where(r => r.ChatId.Equals(chatId));

                        if (recordsetParticipants != null)
                        {
                            db.RemoveRange(recordsetParticipants);
                        }

                        var recordsetPendingQuestions = db.PendingAnonymousQuestions
                                .OrderBy(r => r.ChatId)
                                .Where(r => r.ChatId.Equals(chatId));

                        if (recordsetPendingQuestions != null)
                        {
                            db.RemoveRange(recordsetPendingQuestions);
                        }

                        var recordsetPendingAnswers = db.PendingAnonymousAnswers
                                .OrderBy(r => r.ChatId)
                                .Where(r => r.ChatId.Equals(chatId));

                        if (recordsetPendingAnswers != null)
                        {
                            db.RemoveRange(recordsetPendingAnswers);
                        }

                        var recordsetQuestions = db.AnonymousQuestions
                                .OrderBy(r => r.ChatId)
                                .Where(r => r.ChatId.Equals(chatId));

                        if (recordsetQuestions != null)
                        {
                            db.RemoveRange(recordsetQuestions);
                        }

                        await db.SaveChangesAsync();*/
                }
                if (!member.IsBot)
                {
                    int userId = member.Id;
                    string firstname = member.FirstName;
                    string lastname = member.LastName;

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
                                Logger.Log.Debug($"LeftChatMember HANDLER Delete record: (#id={record.Id} #chatId={record.ChatId} #userId={record.UserId}) from db.AskAnonymousParticipants");

                                db.Remove(record);
                                await db.SaveChangesAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error("LeftChatMember HANDLER Error while processing db.AskAnonymousParticipants", ex);
                    }

                    string mention = lastname != null ?
                                $"<a href=\"tg://user?id={userId}\">" + Helper.ConvertTextToHtmlParseMode(firstname) + " " + Helper.ConvertTextToHtmlParseMode(lastname) + "</a>" :
                                $"<a href=\"tg://user?id={userId}\">" + Helper.ConvertTextToHtmlParseMode(firstname) + "</a>";

                    msg = mention + ", покинул Ханство!";

                    Logger.Log.Debug($"LeftChatMember HANDLER SendTextMessage #chatId={chatId} #msg={msg}");

                    await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("LeftChatMember HANDLER ---", ex);
            }
        }
    }
}