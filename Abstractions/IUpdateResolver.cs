using Telegram.Bot.Types;

namespace MafaniaBot.Abstractions
{
    public interface IUpdateResolver : IExecutable, ISupportable<Update>
    {}
}
