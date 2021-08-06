using System.Threading.Tasks;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.Abstractions
{
    public interface IExecutable
    {
        Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis);       
    }
}
