using Telegram.Bot.Types;

namespace MafaniaBot.Abstractions
{
    public abstract class Command : Entity<Message>
    {
        public abstract string pattern { get; }
    }
}
