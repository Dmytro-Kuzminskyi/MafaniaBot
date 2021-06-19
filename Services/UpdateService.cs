using MafaniaBot.Abstractions;
using MafaniaBot.CallbackQueries;
using MafaniaBot.Commands;
using MafaniaBot.Handlers;
using Telegram.Bot.Types;

namespace MafaniaBot.Services
{
    public class UpdateService : IUpdateService
    {
        public UpdateService()
        {
            _commands = new Command[]
            {
                new WordsCommand(),
                new WeatherCommand(),
                //new HelpCommand(),
                new StartCommand()
            };
            _handlers = new Entity<Message>[]
            {
                new WordsGameHandler()
            };
            _callbackQueries = new Entity<CallbackQuery>[]
            {
                new WordsGameStartCallbackQuery()
            };
        }

        private readonly Command[] _commands;
        private readonly Entity<Message>[] _handlers;
        private readonly Entity<CallbackQuery>[] _callbackQueries;

        public Command[] GetCommands() => _commands;
        public Entity<Message>[] GetHandlers() => _handlers;
        public Entity<CallbackQuery>[] GetCallbackQueries() => _callbackQueries;
    }
}
