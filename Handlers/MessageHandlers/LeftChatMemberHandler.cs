using System;
using System.Threading.Tasks;
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
        public override bool Contains(Message message)
        {
            return message.LeftChatMember != null;
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            Message message = update.Message;
            long chatId = message.Chat.Id;
            long userId = message.LeftChatMember.Id;

            try
            {
                if (message.LeftChatMember.Username != Startup.BOT_USERNAME)
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    await db.SetRemoveAsync(new RedisKey($"ChatMembers:{chatId}"), new RedisValue(userId.ToString()));
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }
        }
    }
}
