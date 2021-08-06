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
            return (message.Text == Command ||
                message.Text == (Command + " &activate") ||
                message.Text == (Command + Startup.BOT_USERNAME) ||
                message.Text == (Command + Startup.BOT_USERNAME + " &activate")) &&
                message.Chat.Type == ChatType.Private;
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            Message message = update.Message;
            long chatId = message.Chat.Id;
            long userId = message.From.Id;
            string firstname = message.From.FirstName;
            string msg;

            if (update.Message.Text == (Command + " &activate"))
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
