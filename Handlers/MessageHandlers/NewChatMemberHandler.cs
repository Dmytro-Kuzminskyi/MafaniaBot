using System;
using System.Threading.Tasks;
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
        public override bool Contains(Message message)
        {
            return message.NewChatMembers != null;
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            Message message = update.Message;
            long chatId = message.Chat.Id;

            await Task.Run(() =>
            {
                try
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    Parallel.ForEach(message.NewChatMembers, async member =>
                    {
                        if (member.Username != Startup.BOT_USERNAME)
                        {
                            await db.SetAddAsync(new RedisKey($"ChatMembers:{chatId}"), new RedisValue(member.Id.ToString()));

                            if (!await db.HashExistsAsync(new RedisKey($"CallUserIcons:{chatId}"), new RedisValue(member.Id.ToString())))
                            {

                                var icon = BaseDictionary.Icons.RandomElement();
                                var hashEntry = new HashEntry(new RedisValue(member.Id.ToString()), new RedisValue(icon));

                                await db.HashSetAsync(new RedisKey($"CallUserIcons:{chatId}"), new HashEntry[] { hashEntry });
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
                }
            });
        }
    }
}
