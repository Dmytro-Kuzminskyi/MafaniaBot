using System.Threading.Tasks;
using MafaniaBot.Models;
using Telegram.Bot.Types;

namespace MafaniaBot.Abstractions
{
    public interface IUpdateService
    {
        public ScopedCommand[] Commands { get; }
        Task ProcessUpdate(Update update);
    }
}
