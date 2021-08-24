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
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Handlers.MessageHandlers
{
    /// <summary>
    /// Triggered when user sends message to group
    /// </summary>
    public sealed class GroupMessageHandler : Handler<Message>
    {
        public override bool Supported(Message message)
        {
            return message.Chat.Type != ChatType.Private;
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis, ITranslateService translateService)
        {
            try
            {
                IDatabaseAsync db = redis.GetDatabase();
                Message message = update.Message;
                long chatId = message.Chat.Id;
                long userId = message.From.Id;

                if (!await db.HashExistsAsync($"MyGroup:{chatId}", "LanguageCode"))
                {
                    await db.HashSetAsync($"MyGroup:{chatId}", new[] { new HashEntry("LanguageCode", translateService.SupportedLanguages.First()) });

                    var chatAdmins = await botClient.GetChatAdministratorsAsync(chatId);

                    foreach (var admin in chatAdmins)
                    {
                        var icon = BaseDictionary.CallIcons.RandomElement();

                        await db.HashSetAsync($"ChatMember:{chatId}:{admin.User.Id}", new[] { new HashEntry("CallIcon", icon) });
                    }
                }

                if (!await db.HashExistsAsync($"ChatMember:{chatId}:{userId}", "CallIcon"))
                {
                    var icon = BaseDictionary.CallIcons.RandomElement();

                    await db.HashSetAsync($"ChatMember:{chatId}:{userId}", new[] { new HashEntry("CallIcon", icon) });
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: error!", ex);
            }
        }
    }
}
