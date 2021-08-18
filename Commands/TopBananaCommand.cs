using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    public class TopBananaCommand : Command
    {
        public TopBananaCommand()
        {
            Command = "/topbanana";
            Description = "Список лучших бананов";
        }

        public override bool Contains(Message message)
        {
            return message.Text.Contains(Command) &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == Command.Length).Any() ||
                    message.Text.Contains($"{Command}@{Startup.BOT_USERNAME}") &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == $"{Command}@{Startup.BOT_USERNAME}".Length).Any();
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            Message message = update.Message;
            long chatId = message.Chat.Id;
            SortedSetEntry[] listBabanasUserInfo = null;

            var text = TextFormatter.GetTextWithoutCommand(message.Text, Command);
            long.TryParse(string.Join(string.Empty, Regex.Split(text, "[^0-9]+")), out var userInputCount);

            userInputCount = userInputCount == 0 ? 10 : userInputCount;

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                var count = await db.SortedSetLengthAsync($"TopBanana");

                if (count < userInputCount)
                    userInputCount = count;

                listBabanasUserInfo = await db.SortedSetRangeByScoreWithScoresAsync($"TopBanana", take: userInputCount > 10 ? 10 : userInputCount, order: Order.Descending);            
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }

            var msg = $"<b>Топ-{userInputCount} бананов</b>\n\n";
            var n = 1;

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

            if (userInputCount > 10)
            {
                msg += $"1/{Math.Ceiling((decimal)userInputCount / 10)}";

                var nextBtn = InlineKeyboardButton.WithCallbackData("⏩", $"{TopBananaCallbackQueryHandler.CallbackOperation}1&2&{userInputCount}");
                var keyboard = new InlineKeyboardMarkup(nextBtn);

                await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html, replyMarkup: keyboard);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);
            }
        }
    }
}
