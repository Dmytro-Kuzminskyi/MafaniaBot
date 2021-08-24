using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
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
        public override bool Supported(ChatMemberUpdated chatMemberUpdated)
        {
            return chatMemberUpdated.Chat.Type != ChatType.Private;
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
                    await db.HashSetAsync($"MyGroup:{chatId}", new[] { new HashEntry("LanguageCode", langCode) });

                    var chatAdmins = await botClient.GetChatAdministratorsAsync(chatId);

                    foreach (var admin in chatAdmins)
                    {
                        var icon = BaseDictionary.CallIcons.RandomElement();

                        await db.HashSetAsync($"ChatMember:{chatId}:{admin.User.Id}", new[] { new HashEntry("CallIcon", icon) });
                    }
                }
                else
                {
                    await db.KeyDeleteAsync($"MyGroup:{chatId}");

                    var chatMembersResult = (RedisKey[])await db.ExecuteAsync("KEYS", $"ChatMember:{chatId}:*");
                    
                    foreach (var key in chatMembersResult)
                        await db.KeyDeleteAsync(key);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: error!", ex);
            }
        }
    }
}
