using System.Collections.Generic;
using MafaniaBot.Abstractions;
using MafaniaBot.Commands;
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
                new AskMenuCommand(),
                new AskCommand(),
                new WeatherCommand(),
                new HelpCommand(),
                new StartCommand(),
            };
            _handlers = new List<Entity<Message>>
            {
                new AskAnonymousHandler(),
                new NewChatMemberHandler(),
                new LeftChatMemberHandler()
            };
            _callbackQueries = new List<Entity<CallbackQuery>>
            {
                new AskSelectUserCallbackQuery(),
                new CancelQuestionCallbackQuery(),
                new CancelAnswerCallbackQuery(),
                new ShowQuestionCallbackQuery(),
                new AnswerQuestionCallbackQuery(),
                new AskActivateCallbackQuery(),
                new AskDeactivateCallbackQuery(),
            };
        }

        public List<Command> GetCommands() => _commands;
        public List<Entity<Message>> GetHandlers() => _handlers;
        public List<Entity<CallbackQuery>> GetCallbackQueries() => _callbackQueries;
    }
}