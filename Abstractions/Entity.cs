using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.Abstractions
{
    public abstract class Entity
    {
        public abstract Task Execute(Message message, ITelegramBotClient botClient);

        public abstract bool Contains(Message message);
    }
}