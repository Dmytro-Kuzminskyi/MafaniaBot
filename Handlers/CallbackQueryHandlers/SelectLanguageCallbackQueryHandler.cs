using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Handlers.CallbackQueryHandlers
{
    public class SelectLanguageCallbackQueryHandler : Handler<CallbackQuery>
    {
        public static string CallbackOperation = "select_language&";

        public override bool Supported(CallbackQuery callbackQuery)
        {
            return callbackQuery.Data.StartsWith(CallbackOperation);
        }
        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis, ITranslateService translateService)
        {
            try
            {
                IDatabaseAsync db = redis.GetDatabase();
                CallbackQuery callbackQuery = update.CallbackQuery;
                ChatType chatType = callbackQuery.Message.Chat.Type;
                long chatId = callbackQuery.Message.Chat.Id;
                int messageId = callbackQuery.Message.MessageId;
                long userId = callbackQuery.From.Id;            

                var langCode = chatType == ChatType.Private
                    ? await db.HashGetAsync($"MyChatMember:{chatId}", "LanguageCode")
                    : await db.HashGetAsync($"MyGroup:{chatId}", "LanguageCode");

                var callbackParams = callbackQuery.Data.Replace(CallbackOperation, string.Empty).Split('&');
                var selectedLangCode = callbackParams.First();
                var expectedUserId = long.Parse(callbackParams.Last());

                if (userId != expectedUserId)
                {
                    await botClient.AnswerCallbackQueryAsync(
                        callbackQueryId: callbackQuery.Id,
                        text: $"{translateService.GetResource("CreatorOnlyString", langCode)}.",
                        cacheTime: 5);

                    return;
                }

                if (chatType == ChatType.Private)
                {
                    await db.HashSetAsync($"MyChatMember:{chatId}", new[] { new HashEntry("LanguageCode", selectedLangCode) });
                }
                else
                {
                    await db.HashSetAsync($"MyGroup:{chatId}", new[] { new HashEntry("LanguageCode", selectedLangCode) });
                }

                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: $"{translateService.GetResource("LanguageChangedString", selectedLangCode)} <b>{translateService.GetResource(selectedLangCode, selectedLangCode)}</b>!",
                    parseMode: ParseMode.Html);
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }
        }
    }
}
