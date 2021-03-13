using System.Collections.Generic;
using MafaniaBot.Abstractions;
using MafaniaBot.Commands;
using MafaniaBot.Commands.AskAnonymous;
using MafaniaBot.Handlers;
using MafaniaBot.CallbackQueries.AskAnonymous;

namespace MafaniaBot.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly List<Command> _commands;
        private readonly List<Entity> _handlers;
        private readonly List<Entity> _callbackQueries;
        public UpdateService()
        {
            _commands = new List<Command>
            {
                new StartCommand(),
                new WeatherCommand(),
                new AskRegCommand(),
                new AskUnregCommand(),
                new AskAnonymousCommand()
            };
            _handlers = new List<Entity>
            { 
                new NewChatMemberHandler()
            };
            _callbackQueries = new List<Entity>
            {
                new AskAnonymousSelectUserCallbackQuery()
            };
        }

        public List<Command> GetCommands() => _commands;
        public List<Entity> GetHandlers() => _handlers;
        public List<Entity> GetCallbackQueries() => _callbackQueries;
    }
}