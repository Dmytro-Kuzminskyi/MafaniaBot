using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.Handlers.MessageHandlers
{
    /// <summary>
    /// Triggered when user/bot leave group
    /// </summary>
    public sealed class LeftChatMemberHandler : Handler<Message>
    {
        public override bool Supported(Message message)
        {
            return message.LeftChatMember != null;
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis, ITranslateService translateService)
        {
            try
            {
                IDatabaseAsync db = redis.GetDatabase();
                Message message = update.Message;
                long chatId = message.Chat.Id;
                long userId = message.LeftChatMember.Id;

                await db.KeyDeleteAsync($"ChatMember:{chatId}:{userId}");
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: error!", ex);
            }
        }
    }
}
