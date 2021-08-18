using System;
using System.Linq;
using System.Threading.Tasks;
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
    public class TopBananaCallbackQueryHandler : Handler<CallbackQuery>
    {
        public static string CallbackOperation = "top_banana&";

        public override bool Contains(CallbackQuery callbackQuery)
        {
            return callbackQuery.Data.StartsWith(CallbackOperation);
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            CallbackQuery callbackQuery = update.CallbackQuery;
            long chatId = callbackQuery.Message.Chat.Id;
            int messageId = callbackQuery.Message.MessageId;
            SortedSetEntry[] listBabanasUserInfo = null;

            var callbackParams = callbackQuery.Data.Replace(CallbackOperation, string.Empty).Split('&');

            var previousPageIndex = int.Parse(callbackParams[0]);
            var currentPageIndex = int.Parse(callbackParams[1]);
            long count = long.Parse(callbackParams.Last());
            var maxPageIndex = Math.Ceiling((decimal)count / 10);

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                listBabanasUserInfo = await db.SortedSetRangeByScoreWithScoresAsync($"TopBanana", skip: currentPageIndex * 10 - 10,
                                          take: count > (currentPageIndex * 10) ? 10 : (count - previousPageIndex * 10), order: Order.Descending);
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }

            var msg = $"<b>Топ-{count} бананов</b>\n\n";
            var n = currentPageIndex * 10 - 9;

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                foreach (var bananaUserInfo in listBabanasUserInfo)
                {
                    var userName = (await db.HashGetAsync($"Banana:{bananaUserInfo.Element}", "Name")).ToString();
                    var lengthText = bananaUserInfo.Score.ToString("n2");

                    msg += $"{n++}. {userName} - {lengthText} см\n";
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
                msg = $"Error while forming the list!";
            }

            msg += $"{currentPageIndex}/{Math.Ceiling((decimal)count / 10)}";

            InlineKeyboardButton backBtn = null;
            InlineKeyboardButton nextBtn = null;

            if (currentPageIndex != 1)
                backBtn = InlineKeyboardButton.WithCallbackData("⏪", $"{CallbackOperation}{currentPageIndex}&{currentPageIndex - 1}&{count}");

            if (currentPageIndex != maxPageIndex)
                nextBtn = InlineKeyboardButton.WithCallbackData("⏩", $"{CallbackOperation}{currentPageIndex}&{currentPageIndex + 1}&{count}");
            
            var keyboard = new InlineKeyboardMarkup(new[] { backBtn, nextBtn }.Where(e => e != null));

            await botClient.EditMessageTextAsync(chatId, messageId, msg, parseMode: ParseMode.Html, replyMarkup: keyboard);
        }
    }
}
