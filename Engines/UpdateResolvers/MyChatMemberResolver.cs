using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Services;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Engines.UpdateResolvers
{
    public class MyChatMemberResolver : IUpdateResolver
    {
        public bool Supported(Update update)
        {
            return update.MyChatMember != null && update.MyChatMember.Chat.Type != ChatType.Channel && !update.MyChatMember.From.IsBot;
        }

        public async Task Resolve(Update update, ITelegramBotClient telegramBotClient, IConnectionMultiplexer connectionMultiplexer)
        {
            await Task.Run(() => 
                Parallel.ForEach(UpdateService.Instance.MyChatMemberHandlers, async handler =>
                {
                    if (handler.Contains(update.MyChatMember))
                    {
                        Logger.Log.Info($"Executing {handler.GetType().Name}. Request: {update}");
                        await handler.Execute(update, telegramBotClient, connectionMultiplexer);
                    }
                }));
        }        
    }
}
