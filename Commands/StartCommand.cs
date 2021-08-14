using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Commands
{
    public sealed class StartCommand : Command
    {
        public StartCommand()
        {
            Command = "/start";
            Description = "Старт";
        }

        public override bool Contains(Message message)
        {
            return message.Chat.Type == ChatType.Private &&
                    (message.Text.Contains(Command) ||
                    message.Text.Contains($"{Command} &activate")) &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == Command.Length).Any() ||
                    (message.Text.Contains($"{Command}@{Startup.BOT_USERNAME}") ||
                    message.Text.Contains($"{Command}@{Startup.BOT_USERNAME} &activate")) &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == $"{Command}@{Startup.BOT_USERNAME}".Length).Any();
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            Message message = update.Message;
            long chatId = message.Chat.Id;
            long userId = message.From.Id;
            string firstname = message.From.FirstName;
            string msg;

            if (message.Text.Contains($"{Command} &activate") || message.Text.Contains($"{Command}@{Startup.BOT_USERNAME} &activate"))
            {
                msg = $"Привет, {firstname}!\n" +
                    $"Теперь ты можешь играть в игры!\n" +
                    $"Если нуждаешься в помощи по командам — введи /help.";

                await botClient.SendTextMessageAsync(chatId, msg);
            }
            else
            {
                msg = $"Привет, {firstname}!\n" +
                    $"Если нуждаешься в помощи по командам — введи /help.\n" +
                    $"👇Играй вместе с друзьями👇";

                var addBtn = InlineKeyboardButton.WithUrl("Добавить в группу", Startup.BOT_URL + $"?startgroup={userId}&invite");
                var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { addBtn } });

                await botClient.SendTextMessageAsync(chatId, msg, replyMarkup: keyboard);
            }
        }
    }
}
