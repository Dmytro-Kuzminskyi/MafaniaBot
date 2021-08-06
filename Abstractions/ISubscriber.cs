using MafaniaBot.Models;
using Telegram.Bot;

namespace MafaniaBot.Abstractions
{
    public interface ISubscriber
    {
        void Subscribe(Game game, ITelegramBotClient telegramBotClient);

        void Unsubscribe(Game game);
    }
}
