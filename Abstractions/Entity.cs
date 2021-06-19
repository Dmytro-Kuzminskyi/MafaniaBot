using StackExchange.Redis;
using System.Threading.Tasks;
using Telegram.Bot;

namespace MafaniaBot.Abstractions
{
    public abstract class Entity<T>
    {
        public abstract Task Execute(T update, ITelegramBotClient botClient, IConnectionMultiplexer redis, IlocalizeService localizer);

        public abstract bool Contains(T update);
    }
}