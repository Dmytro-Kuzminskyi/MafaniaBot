using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.Services.UpdateResolvers
{
    public class CallbackQueryResolver : IUpdateResolver
    {
        private readonly Handler<CallbackQuery>[] callbackQueryHandlers;
        public CallbackQueryResolver(Handler<CallbackQuery>[] callbackQueryHandlers)
        {
            this.callbackQueryHandlers = callbackQueryHandlers;
        }

        public bool Supported(Update update)
        {
            return update.CallbackQuery != null &&
                !update.CallbackQuery.IsGameQuery &&
                !update.CallbackQuery.From.IsBot;
        }

        public async Task Execute(Update update, ITelegramBotClient telegramBotClient, IConnectionMultiplexer connectionMultiplexer, ITranslateService translateService)
        {
            await Task.Run(() =>
                Parallel.ForEach(callbackQueryHandlers, async handler =>
                {
                    if (handler.Supported(update.CallbackQuery))
                    {
                        Logger.Log.Info($"Executing {handler.GetType().Name}. Request: {update}");

                        await handler.Execute(update, telegramBotClient, connectionMultiplexer, translateService);
                    }
                }));
        }        
    }
}
