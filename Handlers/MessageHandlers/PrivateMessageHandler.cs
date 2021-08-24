using System.Threading.Tasks;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MafaniaBot.Abstractions;
using System;

namespace MafaniaBot.Handlers.MessageHandlers
{
    /// <summary>
    /// Triggered when user sends message to chat with bot
    /// </summary>
    public sealed class PrivateMessageHandler : Handler<Message>
    {
        public override bool Supported(Message message)
        {
            return message.Chat.Type == ChatType.Private;
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis, ITranslateService translateService)
        {
            try
            {
                IDatabaseAsync db = redis.GetDatabase();
                Message message = update.Message;
                long chatId = message.Chat.Id;
                string langCode = message.From.LanguageCode;

                if (!await db.HashExistsAsync($"MyChatMember:{chatId}", "LanguageCode"))
                    await db.HashSetAsync($"MyChatMember:{chatId}", new[] { new HashEntry("LanguageCode", langCode) });
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: error!", ex);
            }
        }
    }
}
