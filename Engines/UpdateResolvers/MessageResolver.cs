using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Services;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Engines.UpdateResolvers
{
    public class MessageResolver : IUpdateResolver
    {
        public bool Supported(Update update)
        {
            return update.Message.Chat.Type != ChatType.Channel && !update.Message.From.IsBot;
        }

        public async Task Resolve(Update update, ITelegramBotClient telegramBotClient, IConnectionMultiplexer connectionMultiplexer)
        {
            var updateService = UpdateService.Instance;
            Task botCommandTask = null;

            if (update.Message.Entities?.Where(e => e.Type == MessageEntityType.BotCommand).Any() ?? false)
            {
                botCommandTask = Task.Run(() =>
                Parallel.ForEach(updateService.Commands.Keys, async handler =>
                {
                    if (handler.Contains(update.Message))
                    {
                        Logger.Log.Info($"Executing {handler.GetType().Name}. Request: {update}");
                        await handler.Execute(update, telegramBotClient, connectionMultiplexer);
                    }
                }));
            }

            var messageHandlerTask = Task.Run(() =>
            Parallel.ForEach(updateService.MessageHandlers, async handler =>
            {
                if (handler.Contains(update.Message))
                {
                    Logger.Log.Info($"Executing {handler.GetType().Name}. Request: {update}");
                    await handler.Execute(update, telegramBotClient, connectionMultiplexer);
                }
            }));

            await Task.WhenAll(new[] { botCommandTask, messageHandlerTask }.Where(e => e != null));
        }
    }
}
