using Telegram.Bot.Types;

namespace MafaniaBot.Abstractions
{
    public interface IUpdateService
    {
        Command[] GetCommands();
        Entity<Message>[] GetHandlers();
        Entity<CallbackQuery>[] GetCallbackQueries();
    }
}
