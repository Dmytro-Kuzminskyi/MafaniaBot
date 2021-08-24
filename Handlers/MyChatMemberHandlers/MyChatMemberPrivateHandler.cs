using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Handlers.MyChatMemberHandlers
{
    /// <summary>
    /// Triggered when user initiate private chat with bot or block bot
    /// </summary>
    public sealed class MyChatMemberPrivateHandler : Handler<ChatMemberUpdated>
    {
        public override bool Supported(ChatMemberUpdated chatMemberUpdated)
        {
            return chatMemberUpdated.Chat.Type == ChatType.Private;
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis, ITranslateService translateService)
        {
            try
            {
                IDatabaseAsync db = redis.GetDatabase();
                ChatMemberUpdated myChatMember = update.MyChatMember;
                ChatMemberStatus status = myChatMember.NewChatMember.Status;
                long chatId = myChatMember.Chat.Id;
                string langCode = myChatMember.From.LanguageCode;

                if (status == ChatMemberStatus.Member)
                {
                    await db.HashSetAsync($"MyChatMember:{chatId}", new[] { new HashEntry ("LanguageCode", langCode)});
                }
                else
                {
                    await db.KeyDeleteAsync($"MyChatMember:{chatId}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: error!", ex);
            }
        }
    }
}
