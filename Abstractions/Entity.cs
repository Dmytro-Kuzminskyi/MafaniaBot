using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.Abstractions
{
    public abstract class Entity<T>
    {
        public abstract Task Execute(T update, ITelegramBotClient botClient);

        public abstract bool Contains(Message message);
    }
}