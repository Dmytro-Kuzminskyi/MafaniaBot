using System.Threading.Tasks;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.Abstractions
{
    public interface IUpdateResolver
    {
        bool Supported(Update update);
        Task Resolve(Update update, ITelegramBotClient telegramBotClient, IConnectionMultiplexer connectionMultiplexer);
    }
}
