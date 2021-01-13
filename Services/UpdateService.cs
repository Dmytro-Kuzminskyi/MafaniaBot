using System.Collections.Generic;
using MafaniaBot.Abstractions;
using MafaniaBot.Commands;
using MafaniaBot.Handlers;

namespace MafaniaBot.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly List<Command> _commands;
        private readonly List<Entity> _handlers;
        public UpdateService()
        {
            _commands = new List<Command>
            {
                new StartCommand(),
                new WeatherCommand(),
                new AskAnonymousCommand()
            };
            _handlers = new List<Entity>
            { 
                new NewChatMemberHandler()
            };
        }

        public List<Command> GetCommands() => _commands;
        public List<Entity> GetEntities() => _handlers;
    }
}