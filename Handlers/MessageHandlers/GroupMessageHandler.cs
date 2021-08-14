using System;
using System.Threading.Tasks;
using MafaniaBot.Dictionaries;
using MafaniaBot.Extensions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Handlers.MessageHandlers
{
    /// <summary>
    /// Triggered when user sends message to group
    /// </summary>
    public sealed class GroupMessageHandler : Handler<Message>
    {
        public override bool Contains(Message message)
        {
            return message.Chat.Type != ChatType.Private;
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            Message message = update.Message;
            long chatId = message.Chat.Id;
            long userId = message.From.Id;

            try
            {
                if (message.From.Username != Startup.BOT_USERNAME)
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    if (await db.SetAddAsync(new RedisKey($"ChatMembers:{chatId}"), new RedisValue(userId.ToString())))
                    {
                        var icon = BaseDictionary.Icons.RandomElement();
                        var hashEntry = new HashEntry(new RedisValue(userId.ToString()), new RedisValue(icon));

                        await db.HashSetAsync(new RedisKey($"CallUserIcons:{chatId}"), new HashEntry[] { hashEntry });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }
        }
    }
}
