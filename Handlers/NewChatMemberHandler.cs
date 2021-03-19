using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Handlers
{
    public class NewChatMemberHandler : Entity<Message>
    {
        public override bool Contains(Message message)
        {
            if (message.Chat.Type == ChatType.Private || message.Chat.Type == ChatType.Channel)
                return false;

            return (message.NewChatMembers != null) ? true : false;
        }

        public override async Task Execute(Message message, ITelegramBotClient botClient)
        {           
            long chatId = message.Chat.Id;
            User user = message.NewChatMembers[0];

            Logger.Log.Debug($"NewChatMember HANDLER triggered in #chatId={chatId} #memberId={user.Id}");

            string msg = null;

            try
            {
                if (user.Id.Equals(botClient.BotId))
                {
                    msg += "\n/help - список доступных команд.";

                    Logger.Log.Debug($"NewChatMember HANDLER SendTextMessage #chatId={chatId} #msg={msg}");
                    await Task.Delay(3000);
                    await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Markdown);
                }
                else
                {
                    string firstname = user.FirstName;
                    string lastname = user.LastName;
                    int userId = user.Id;

                    if (!user.IsBot)
                    {
                        string mention = lastname != null ?
                            "[" + firstname + " " + lastname + "](tg://user?id=" + userId + ")" :
                            "[" + firstname + "](tg://user?id=" + userId + ")";

                        msg += mention + ", добро пожаловать в Ханство!";

                        Logger.Log.Debug($"NewChatMember HANDLER SendTextMessage #chatId={chatId} #msg={msg}");
                        await Task.Delay(3000);
                        await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Markdown);
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("NewChatMember HANDLER Error while SendTextMessage", ex);
            }
        }
    }
}