using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.Models
{
    public abstract class Command : BotCommand, IExecutable, IContainable<Message>
    {
        public abstract bool Contains(Message update);

        public abstract Task Execute(Update update, ITelegramBotClient botClient, StackExchange.Redis.IConnectionMultiplexer redis);
    }
}
