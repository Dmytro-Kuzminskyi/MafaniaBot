using System.Threading.Tasks;
using StackExchange.Redis;
using Telegram.Bot;

namespace MafaniaBot.Abstractions
{
    public abstract class Entity<T>
    {
        public abstract Task Execute(T update, ITelegramBotClient botClient, IConnectionMultiplexer redis);

        public abstract bool Contains(T update);
    }
}