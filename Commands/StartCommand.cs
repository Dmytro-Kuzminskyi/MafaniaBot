using System.Threading.Tasks;
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
            if (message.Chat.Type != ChatType.Private)
                return false;

            return message.Text.StartsWith(pattern) && !message.From.IsBot;
        }

        public override async Task Execute(Message message, ITelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            var userId = message.From.Id;
            var firstname = message.From.FirstName;
            var lastname = message.From.LastName;

            var mention = lastname != null ? 
                "[" + firstname + " " + lastname + "](tg://user?id=" + userId + ")" :
                "[" + firstname + "](tg://user?id=" + userId + ")";

            var msg = "Привет, " + mention + "!" + 
                "\nЧтобы использовать команды бота, добавь его в группу." +
                "\n/help - список доступных команд.";

            await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Markdown);
        }
    }
}