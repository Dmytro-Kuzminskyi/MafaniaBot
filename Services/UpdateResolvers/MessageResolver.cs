using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Services.UpdateResolvers
{
    public class MessageResolver : IUpdateResolver
    {
        private readonly ScopedCommand[] commands;
        private readonly Handler<Message>[] messageHandlers;

        public MessageResolver(ScopedCommand[] commands, Handler<Message>[] messageHandlers)
        {
            this.commands = commands;
            this.messageHandlers = messageHandlers;
        }

        public bool Supported(Update update)
        {
            return update.Message.Chat.Type != ChatType.Channel &&
                !update.Message.From.IsBot;
        }

        public async Task Execute(Update update, ITelegramBotClient telegramBotClient, IConnectionMultiplexer connectionMultiplexer, ITranslateService translateService)
        {
            Task botCommandTask = null;

            if (update.Message.Entities?.Where(e => e.Type == MessageEntityType.BotCommand).Any() ?? false)
            {
                botCommandTask = Task.Run(() =>
                Parallel.ForEach(commands, async command =>
                {
                    if (command.Supported(update.Message))
                    {
                        Logger.Log.Info($"Executing {command.GetType().Name}. Request: {update}");

                        await command.Execute(update, telegramBotClient, connectionMultiplexer, translateService);
                    }
                }));
            }

            var messageHandlerTask = Task.Run(() =>
            Parallel.ForEach(messageHandlers, async handler =>
            {
                if (handler.Supported(update.Message))
                {
                    Logger.Log.Info($"Executing {handler.GetType().Name}. Request: {update}");

                    await handler.Execute(update, telegramBotClient, connectionMultiplexer, translateService);
                }
            }));

            await Task.WhenAll(new[] { botCommandTask, messageHandlerTask }.Where(e => e != null));
        }
    }
}
