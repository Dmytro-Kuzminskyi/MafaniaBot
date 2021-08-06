using System.Collections.Generic;
using MafaniaBot.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Abstractions
{
    public interface IUpdateService
    {
        Dictionary<Command, BotCommandScopeType> GetCommands();

        IContainable<Message>[] GetMessageHandlers();

        IContainable<CallbackQuery>[] GetCallbackQueries();

        IExecutable GetMyChatMemberHandler();
    }
}
