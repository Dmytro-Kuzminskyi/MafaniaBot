using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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

        public override async Task Execute(Message message, ITelegramBotClient botClient)
        {            
            long chatId = message.Chat.Id;
            User member = message.LeftChatMember;

            Logger.Log.Debug($"LeftChatMember HANDLER triggered in #chatId={chatId} #memberId={member.Id}");

            string msg = null;

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
                            db.AskAnonymousParticipants.Remove(record);
                            await db.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Error("LeftChatMember HANDLER Error while processing database", ex);
                }

                string mention = lastname != null ?
                            "[" + firstname + " " + lastname + "](tg://user?id=" + userId + ")" :
                            "[" + firstname + "](tg://user?id=" + userId + ")";

                msg += mention + ", покинул Ханство!";

                try
                {
                    Logger.Log.Debug($"LeftChatMember HANDLER SendTextMessage #chatId={chatId} #msg={msg}");
                    await Task.Delay(3000);
                    await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Markdown);
                }
                catch (Exception ex)
                {
                    Logger.Log.Error("LeftChatMember HANDLER Error while SendTextMessage", ex);
                }
            }
        }
    }
}