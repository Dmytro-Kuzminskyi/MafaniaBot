using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.Abstractions
{
    public abstract class Command : Entity
    {
        public abstract string pattern { get; }
    }
}
