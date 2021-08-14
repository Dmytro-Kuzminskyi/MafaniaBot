using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.Models
{
    public abstract class Handler<T> : IExecutable, IContainable<T>  where T : class
    {
        public abstract bool Contains(T update);
        public abstract Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis);
    }
}
