using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Services;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.Engines.UpdateResolvers
{
    public class CallbackQueryResolver : IUpdateResolver
    {
        public bool Supported(Update update)
        {
            return update.CallbackQuery != null && !update.CallbackQuery.IsGameQuery && !update.CallbackQuery.From.IsBot;
        }

        public async Task Resolve(Update update, ITelegramBotClient telegramBotClient, IConnectionMultiplexer connectionMultiplexer)
        {
            await Task.Run(() =>
                Parallel.ForEach(UpdateService.Instance.CallbackQueryHandlers, async handler =>
                {
                    if (handler.Contains(update.CallbackQuery))
                    {
                        Logger.Log.Info($"Executing {handler.GetType().Name}. Request: {update}");
                        await handler.Execute(update, telegramBotClient, connectionMultiplexer);
                    }
                }));
        }        
    }
}
