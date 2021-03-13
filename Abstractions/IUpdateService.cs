using System.Collections.Generic;
using Telegram.Bot.Types;

namespace MafaniaBot.Abstractions
{
    public interface IUpdateService
    {
        List<Command> GetCommands();
        List<Entity<Message>> GetHandlers();
        List<Entity<CallbackQuery>> GetCallbackQueries();
    }
}
