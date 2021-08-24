using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Dictionaries;
using MafaniaBot.Extensions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.Handlers.MessageHandlers
{
    /// <summary>
    /// Triggered when user/bot join group
    /// </summary>
    public sealed class NewChatMemberHandler : Handler<Message>
    {
        public override bool Supported(Message message)
        {
            return !message.NewChatMembers?.First().IsBot ?? false;
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis, ITranslateService translateService)
        {
            try
            {
                IDatabaseAsync db = redis.GetDatabase();
                Message message = update.Message;
                User newChatMember = message.NewChatMembers.First();
                long chatId = message.Chat.Id;
                var icon = BaseDictionary.CallIcons.RandomElement();

                await db.HashSetAsync($"ChatMember:{chatId}:{newChatMember.Id}", new[] { new HashEntry("CallIcon", icon) });
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: error!", ex);
            }
        }
    }
}
