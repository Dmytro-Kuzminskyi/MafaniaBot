using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Handlers.CallbackQueryHandlers
{
    public class LanguageBackCallbackQueryHandler : Handler<CallbackQuery>
    {
        public static string CallbackOperation = "language_back&";

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

                var expectedUserId = long.Parse(callbackQuery.Data.Replace(CallbackOperation, string.Empty));

                if (userId != expectedUserId)
                {
                    await botClient.AnswerCallbackQueryAsync(
                        callbackQueryId: callbackQuery.Id,
                        text: $"{translateService.GetResource("CreatorOnlyString", langCode)}.",
                        cacheTime: 5);

                    return;
                }

                var langBtn = InlineKeyboardButton.WithCallbackData($"{translateService.GetResource("LanguageString", langCode)}", $"{LanguageCallbackQueryHandler.CallbackOperation}{userId}");
                var exitBtn = InlineKeyboardButton.WithCallbackData($"{translateService.GetResource("ExitString", langCode)}", $"{SettingsExitCallbackQueryHandler.CallbackOperation}{userId}");
                var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { langBtn }, new InlineKeyboardButton[] { exitBtn } });

                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: $"{translateService.GetResource("MenuString", langCode)}",
                    replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: error!", ex);
            }
        }
    }
}
