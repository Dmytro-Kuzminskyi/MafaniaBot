using Telegram.Bot.Types;

namespace MafaniaBot.Abstractions
{
    public abstract class Command : Entity<Message>
    {
        public abstract string Pattern { get; }

        public abstract string Description { get; }
    }
}
