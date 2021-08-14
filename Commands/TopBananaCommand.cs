using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
            SortedSetEntry[] listBabanasInfo = null;
            int count = 10;

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                listBabanasInfo = await db.SortedSetRangeByScoreWithScoresAsync(new RedisKey($"TopBanana"), take: count, order: Order.Descending);            
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }

            var msg = $"<b>Топ-{count} бананов</b>\n\n";
            var n = 1;

            foreach (var bananaInfo in listBabanasInfo)
            {
                var lengthText = bananaInfo.Score.ToString("n2");
                msg += $"{n++}. {bananaInfo.Element} - {lengthText} см\n";
            }

            await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);
        }
    }
}
