using System;
using System.Threading.Tasks;
using MafaniaBot.Dictionaries;
using MafaniaBot.Extensions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Handlers.MyChatMemberHandlers
{
    /// <summary>
    /// Triggered when user add bot to group or delete bot from group
    /// </summary>
    public sealed class MyChatMemberGroupHandler : Handler<ChatMemberUpdated>
    {
        public override bool Contains(ChatMemberUpdated chatMemberUpdated)
        {
            return chatMemberUpdated.Chat.Type != ChatType.Private;
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            ChatMemberUpdated myChatMember = update.MyChatMember;
            ChatMemberStatus status = myChatMember.NewChatMember.Status;
            long chatId = myChatMember.Chat.Id;       

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                if (status == ChatMemberStatus.Member)
                {
                    var chatUserAdmins = await botClient.GetChatAdministratorsAsync(chatId);
                    var setAddTask = db.SetAddAsync(new RedisKey("MyGroups"), new RedisValue(chatId.ToString()));

                    Parallel.ForEach(chatUserAdmins, async userAdmin =>
                    {
                        var user = userAdmin.User;
                        await db.SetAddAsync(new RedisKey($"ChatMembers:{chatId}"), new RedisValue(user.Id.ToString()));

                        if (await db.HashExistsAsync(new RedisKey($"CallUserIcons:{chatId}"), new RedisValue(user.Id.ToString())))
                            return;

                        var icon = BaseDictionary.Icons.RandomElement();
                        var hashEntry = new HashEntry(new RedisValue(user.Id.ToString()), new RedisValue(icon));

                        await db.HashSetAsync(new RedisKey($"CallUserIcons:{chatId}"), new HashEntry[] { hashEntry });
                    });

                    await setAddTask;
                }
                else
                {
                    await db.SetRemoveAsync(new RedisKey("MyGroups"), new RedisValue(chatId.ToString()));
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }
        }
    }
}
