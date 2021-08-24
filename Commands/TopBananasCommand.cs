using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Handlers.CallbackQueryHandlers;
using MafaniaBot.Helpers;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Commands
{
    public class TopBananasCommand : ScopedCommand
    {
        public TopBananasCommand(BotCommandScopeType[] scopeTypes) : base(scopeTypes)
        {
            Command = "/topbananas";
            Description = "Top bananas";
        }

        public override bool Supported(Message message)
        {
            return (message.Text.Contains(Command) &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == Command.Length).Any()) ||
                    (message.Text.Contains($"{Command}@{Startup.BOT_USERNAME}") &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == $"{Command}@{Startup.BOT_USERNAME}".Length).Any());
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis, ITranslateService translateService)
        {
            try
            {
                IDatabaseAsync db = redis.GetDatabase();
                Message message = update.Message;
                long chatId = message.Chat.Id;
                string langCode = string.Empty;
                SortedSetEntry[] listBabanasUserInfo = null;

                langCode = message.Chat.Type == ChatType.Private ? await db.HashGetAsync($"MyChatMember:{chatId}", "LanguageCode")
                                                        : await db.HashGetAsync($"MyGroup:{chatId}", "LanguageCode");

                var text = TextFormatter.GetTextWithoutCommand(message.Text, Command);
                long.TryParse(string.Join(string.Empty, Regex.Split(text, "[^0-9]+")), out var userInputCount);

                userInputCount = userInputCount == 0 ? 10 : userInputCount;

                var count = await db.SortedSetLengthAsync($"TopBananas");

                if (count < userInputCount)
                    userInputCount = count;

                listBabanasUserInfo = await db.SortedSetRangeByScoreWithScoresAsync($"TopBananas", take: userInputCount > 10 ? 10 : userInputCount, order: Order.Descending);

                var msg = $"<b>{translateService.GetResource("TopString", langCode)}-{userInputCount} {translateService.GetResource("BananasString", langCode)}</b>\n\n";
                var n = 1;

                foreach (var bananaUserInfo in listBabanasUserInfo)
                {
                    var userName = (await db.HashGetAsync($"Banana:{bananaUserInfo.Element}", "Name")).ToString();
                    var lengthText = bananaUserInfo.Score.ToString("n2");

                    msg += $"{n++}. {userName} - {lengthText} {translateService.GetResource("CentimetersString", langCode)}\n";
                }

                if (userInputCount > 10)
                {
                    msg += $"1/{Math.Ceiling((decimal)userInputCount / 10)}";

                    var nextBtn = InlineKeyboardButton.WithCallbackData("⏩", $"{TopBananasCallbackQueryHandler.CallbackOperation}1&2&{userInputCount}");
                    var keyboard = new InlineKeyboardMarkup(nextBtn);

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: msg,
                        parseMode: ParseMode.Html,
                        replyMarkup: keyboard);
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: msg,
                        parseMode: ParseMode.Html);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: SendTextMessageAsync error!", ex);
            }
        }
    }
}
