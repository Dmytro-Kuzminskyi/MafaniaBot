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
    public sealed class TitlesCommand : Command
    {
        public TitlesCommand()
        {
            Command = "/titles";
            Description = "Последние звания чата";
        }

        public override bool Contains(Message message)
        {
            return (message.Text.Contains(Command) &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == Command.Length).Any()) ||
                    (message.Text.Contains($"{Command}@{Startup.BOT_USERNAME}") &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == $"{Command}@{Startup.BOT_USERNAME}".Length).Any());
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            Message message = update.Message;
            long chatId = message.Chat.Id;
            string[] titleStrings = null;

            if (message.Chat.Type == ChatType.Private)
            {
                await botClient.SendTextMessageAsync(chatId, "Эта команда доступна только в групповом чате.");
                return;
            }           

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                titleStrings = (await db.ListRangeAsync(new RedisKey($"Titles:{chatId}"), 0, 9)).ToStringArray();
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }

            var msg = "<b>Последние звания чата</b>\n\n";
            var n = 1;

            foreach (var title in titleStrings)
                msg += $"{n++}. {title}\n";

            await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);
        }
    }
}
