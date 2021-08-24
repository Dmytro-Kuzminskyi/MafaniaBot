using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Services.UpdateResolvers
{
    public class MyChatMemberResolver : IUpdateResolver
    {
        private readonly Handler<ChatMemberUpdated>[] myChatMemberHandlers;

        public MyChatMemberResolver(Handler<ChatMemberUpdated>[] myChatMemberHandlers)
        {
            this.myChatMemberHandlers = myChatMemberHandlers;
        }

        public bool Supported(Update update)
        {
            return update.MyChatMember != null &&
                update.MyChatMember.Chat.Type != ChatType.Channel &&
                !update.MyChatMember.From.IsBot;
        }

        public async Task Execute(Update update, ITelegramBotClient telegramBotClient, IConnectionMultiplexer connectionMultiplexer, ITranslateService translateService)
        {
            await Task.Run(() => 
                Parallel.ForEach(myChatMemberHandlers, async handler =>
                {
                    if (handler.Supported(update.MyChatMember))
                    {
                        Logger.Log.Info($"Executing {handler.GetType().Name}. Request: {update}");

                        await handler.Execute(update, telegramBotClient, connectionMultiplexer, translateService);
                    }
                }));
        }        
    }
}
