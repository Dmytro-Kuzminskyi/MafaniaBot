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
            return message.Text.StartsWith(Command) || message.Text.StartsWith(Command + Startup.BOT_USERNAME);
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            Message message = update.Message;            
        }
    }
}
