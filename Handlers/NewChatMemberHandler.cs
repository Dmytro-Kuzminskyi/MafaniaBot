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
            try
            {
                long chatId = message.Chat.Id;
                User user = message.NewChatMembers[0];
                string msg = null;

                Logger.Log.Debug($"NewChatMember HANDLER triggered in #chatId={chatId} #memberId={user.Id}");

                if (user.Id.Equals(botClient.BotId))
                {
                    msg =
                        "<b>Общие команды</b>\n" +
                        "/weather [city] — узнать текущую погоду.\n\n" +
                        "<b>Команды группового чата</b>\n" +
                        "/askmenu — меню анонимных вопросов.";

                    Logger.Log.Debug($"NewChatMember HANDLER SendTextMessage #chatId={chatId} #msg={msg}");

                    await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);

                    return;
                }

                string firstname = user.FirstName;
                string lastname = user.LastName;
                int userId = user.Id;

                if (!user.IsBot)
                {
                    string mention = lastname != null ?
                        $"<a href=\"tg://user?id={userId}\">" + Helper.ConvertTextToHtmlParseMode(firstname) + " " + Helper.ConvertTextToHtmlParseMode(lastname) + "</a>" :
                        $"<a href=\"tg://user?id={userId}\">" + Helper.ConvertTextToHtmlParseMode(firstname) + "</a>";

                    msg = mention + ", добро пожаловать в Ханство!";

                    Logger.Log.Debug($"NewChatMember HANDLER SendTextMessage #chatId={chatId} #msg={msg}");

                    await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("NewChatMember HANDLER ---", ex);
            }
        }
    }
}