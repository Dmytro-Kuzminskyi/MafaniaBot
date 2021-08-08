using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Handlers
{
    public class NewChatMemberHandler : IExecutable, IContainable<Message>
    {
        public bool Contains(Message message)
        {
            return message.Chat.Type != ChatType.Private && message.NewChatMembers != null;
        }

        public Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            Message message = update.Message;
            long chatId = message.Chat.Id;

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                Parallel.ForEach(message.NewChatMembers, async member =>
                {
                    await db.SetAddAsync(new RedisKey($"ChatMembers:{chatId}"), new RedisValue(member.Id.ToString()));
                });              
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }

            return Task.CompletedTask;
        }
    }
}
