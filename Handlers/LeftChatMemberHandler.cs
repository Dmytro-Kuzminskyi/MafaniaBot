using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Handlers
{
    public class LeftChatMemberHandler : IExecutable, IContainable<Message>
    {
        public bool Contains(Message message)
        {
            return message.Chat.Type != ChatType.Private && message.LeftChatMember != null;
        }

        public async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            Message message = update.Message;
            long chatId = message.Chat.Id;
            long userId = message.LeftChatMember.Id;

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                await db.SetRemoveAsync(new RedisKey($"ChatMembers:{chatId}"), new RedisValue(userId.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }
        }
    }
}
