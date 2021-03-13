using System.Collections.Generic;
using MafaniaBot.Abstractions;
using MafaniaBot.Commands;
using MafaniaBot.Commands.AskAnonymous;
using MafaniaBot.Handlers;
using MafaniaBot.CallbackQueries.AskAnonymous;
using Telegram.Bot.Types;

namespace MafaniaBot.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly List<Command> _commands;
        private readonly List<Entity<Message>> _handlers;
        private readonly List<Entity<CallbackQuery>> _callbackQueries;
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
            _handlers = new List<Entity<Message>>
            { 
                new NewChatMemberHandler(),
                new AskAnonymousHandler()
            };
            _callbackQueries = new List<Entity<CallbackQuery>>
            {
                new AskAnonymousSelectUserCallbackQuery(),
                new ShowAnonymousQuestionCallbackQuery()
            };
        }

        public List<Command> GetCommands() => _commands;
        public List<Entity<Message>> GetHandlers() => _handlers;
        public List<Entity<CallbackQuery>> GetCallbackQueries() => _callbackQueries;
    }
}