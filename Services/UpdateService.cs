using System.Collections.Generic;
using MafaniaBot.Abstractions;
using MafaniaBot.CallbackQueries;
using MafaniaBot.Commands;
using MafaniaBot.Handlers;
using MafaniaBot.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly Dictionary<Command, BotCommandScopeType> commands;
        private readonly IContainable<Message>[] _messageHandlers;
        private readonly IContainable<CallbackQuery>[] _callbackQueries;
        private readonly IExecutable _myChatMemberHandler;

        public UpdateService()
        {
            _myChatMemberHandler = new MyChatMemberHandler();

            commands = new Dictionary<Command, BotCommandScopeType>
            {
                { new ClassicWordsCommand(), BotCommandScopeType.Default },
                //{ new HelpCommand(), BotCommandScopeType.Default },
                { new StartCommand(), BotCommandScopeType.Default }
            };
            _messageHandlers = new IContainable<Message>[]
            {
                new GameMessageHandler()
            };
            _callbackQueries = new IContainable<CallbackQuery>[]
            {
                new ClassicWordsGameStartCallbackQuery()
            };
        }

        public Dictionary<Command, BotCommandScopeType> GetCommands() => commands;
        public IContainable<Message>[] GetMessageHandlers() => _messageHandlers;
        public IContainable<CallbackQuery>[] GetCallbackQueries() => _callbackQueries;
        public IExecutable GetMyChatMemberHandler() => _myChatMemberHandler;
    }
}
