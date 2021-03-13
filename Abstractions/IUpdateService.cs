using System.Collections.Generic;

namespace MafaniaBot.Abstractions
{
    public interface IUpdateService
    {
        List<Command> GetCommands();
        List<Entity> GetHandlers();
        List<Entity> GetCallbackQueries();
    }
}
