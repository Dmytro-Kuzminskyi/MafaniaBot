using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.Commands
{
    public sealed class HelpCommand : Command
    {
        public HelpCommand()
        {
            Command = "/help";
            Description = "Помощь по командам";
        }

        public override bool Contains(Message message)
        {
            return message.Text.Contains(Command) &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == Command.Length).Any() ||
                    message.Text.Contains($"{Command}@{Startup.BOT_USERNAME}") &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == $"{Command}@{Startup.BOT_USERNAME}".Length).Any();
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            Message message = update.Message;            
        }
    }
}
