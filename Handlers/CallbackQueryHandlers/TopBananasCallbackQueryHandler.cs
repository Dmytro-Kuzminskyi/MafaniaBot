using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Commands;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Handlers.CallbackQueryHandlers
{
    /// <summary>
	/// Triggered when user change page of banana list
    /// Params [previous page, current page, count]
	/// </summary>
    public class TopBananasCallbackQueryHandler : Handler<CallbackQuery>
    {
        public static string CallbackOperation = "top_bananas&";

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
                long chatId = callbackQuery.Message.Chat.Id;
                int messageId = callbackQuery.Message.MessageId;
                SortedSetEntry[] listBabanasUserInfo = null;

                var callbackParams = callbackQuery.Data.Replace(CallbackOperation, string.Empty).Split('&');

                var previousPageIndex = int.Parse(callbackParams[0]);
                var currentPageIndex = int.Parse(callbackParams[1]);
                long count = long.Parse(callbackParams.Last());
                var maxPageIndex = Math.Ceiling((decimal)count / 10);

                var langCode = callbackQuery.Message.Chat.Type == ChatType.Private ? await db.HashGetAsync($"MyChatMember:{chatId}", "LanguageCode")
                                                                                    : await db.HashGetAsync($"MyGroup:{chatId}", "LanguageCode");

                listBabanasUserInfo = await db.SortedSetRangeByScoreWithScoresAsync($"TopBananas", skip: currentPageIndex * 10 - 10,
                                          take: count > (currentPageIndex * 10) ? 10 : (count - previousPageIndex * 10), order: Order.Descending);

                var msg = $"<b>{translateService.GetResource("TopString", langCode)}-{count} {translateService.GetResource("BananasString", langCode)}</b>\n\n";
                var n = currentPageIndex * 10 - 9;

                foreach (var bananaUserInfo in listBabanasUserInfo)
                {
                    var userName = (await db.HashGetAsync($"Banana:{bananaUserInfo.Element}", "Name")).ToString();
                    var lengthText = bananaUserInfo.Score.ToString("n2");

                    msg += $"{n++}. {userName} - {lengthText} см\n";
                }

                msg += $"{currentPageIndex}/{Math.Ceiling((decimal)count / 10)}";

                InlineKeyboardButton backBtn = null;
                InlineKeyboardButton nextBtn = null;

                if (currentPageIndex != 1)
                    backBtn = InlineKeyboardButton.WithCallbackData("⏪", $"{CallbackOperation}{currentPageIndex}&{currentPageIndex - 1}&{count}");

                if (currentPageIndex != maxPageIndex)
                    nextBtn = InlineKeyboardButton.WithCallbackData("⏩", $"{CallbackOperation}{currentPageIndex}&{currentPageIndex + 1}&{count}");

                var keyboard = new InlineKeyboardMarkup(new[] { backBtn, nextBtn }.Where(e => e != null));

                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: msg,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }
        }
    }
}
