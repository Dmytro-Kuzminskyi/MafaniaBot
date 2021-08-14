using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Helpers;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Commands
{
    public class BananaCommand : Command
    {
        public BananaCommand()
        {
            Command = "/banana";
            Description = "Отрастить банан";
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
            long userId = message.From.Id;
            int messageId = message.MessageId;
            string msg;

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                var valueWithExpiry = await db.StringGetWithExpiryAsync(new RedisKey($"LastBanana:{userId}"));

                if (!valueWithExpiry.Value.IsNull)
                {
                    var time = valueWithExpiry.Expiry;
                    var hours = time?.Hours;
                    var minutes = time?.Minutes;
                    var seconds = time?.Seconds;
                    msg = $"Следующая попытка будет доступна через";

                    if (hours == 0 && minutes == 0 && seconds == 0)
                    {
                        msg += " 1 сек";
                    }
                    else
                    {
                        if (hours > 0)
                        {
                            msg += $" {hours} ч";
                        }

                        if (minutes > 0)
                        {
                            msg += $" {minutes} мин";
                        }

                        if (seconds > 0)
                        {
                            msg += $" {seconds} сек";
                        }
                    }

                    msg += "!";

                    await botClient.SendTextMessageAsync(chatId, msg, replyToMessageId: messageId);
                    return;
                }
                
                var getChatMembertask = botClient.GetChatMemberAsync(chatId, userId);
                var userData = await db.HashGetAllAsync(new RedisKey($"Banana:{userId}"));

                float.TryParse(userData?.Where(e => e.Name == "Length").Select(e => e.Value).FirstOrDefault(), out var length);
                length = await BananaGrowHandler(length, botClient, chatId, messageId);

                User userInfo = (await getChatMembertask).User;
                var userTag = TextFormatter.GenerateMention(userId, userInfo.FirstName, userInfo.LastName);
                
                var hashEntries = new HashEntry[] 
                {
                    new HashEntry(new RedisValue("UserTag"), new RedisValue(userTag)),
                    new HashEntry(new RedisValue("Length"), new RedisValue(length.ToString("n2")))                
                };

                var hashSetTask = db.HashSetAsync(new RedisKey($"Banana:{userId}"), hashEntries);
                var sortedSetAddTask = db.SortedSetAddAsync(new RedisKey($"TopBanana"), hashEntries.First().Value, length);
                var stringSetTask = db.StringSetAsync(new RedisKey($"LastBanana:{userId}"), new RedisValue(userTag), TimeSpan.FromHours(4));

                await Task.WhenAll(new Task[] { hashSetTask, sortedSetAddTask, stringSetTask });
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }
        }

        private async Task<float> BananaGrowHandler(float? length, ITelegramBotClient botClient, long chatId, int messageId)
        {
            string msg;
            var random = new Random(DateTime.Now.Millisecond);
            var currentLength = length ?? 0;

            float lengthDiff = 30 - currentLength;
            float chanceToGrowInitial = 0.4f;
            float chanceToGrowStep = 0.02f;
            float growMultiplierInitial = 0.5f;
            float growMultiplierStep = 0.05f;

            float totalChanceToGrow = lengthDiff > 0 ? (lengthDiff * chanceToGrowStep) + chanceToGrowInitial : chanceToGrowInitial;
            float totalGrowMultiplier = lengthDiff > 0 ? (lengthDiff * growMultiplierStep) + growMultiplierInitial : growMultiplierInitial;

            var difference = (float)(random.NextDouble() * totalGrowMultiplier);
            var diffText = difference.ToString("n2");

            if (random.NextDouble() <= totalChanceToGrow)
            {
                currentLength += difference;
                var currentText = currentLength.ToString("n2");

                msg = $"Твой 🍌 увеличился на {diffText} см!\n" +
                    $"Теперь его длина составляет {currentText} см!";
            }
            else
            {
                currentLength -= difference;
                currentLength = currentLength < 0 ? 0 : currentLength;
                var currentText = currentLength.ToString("n2");

                msg = $"Твой 🍌 уменьшился на {diffText} см!\n" +
                    $"Теперь его длина составляет {currentText} см!";               
            }

            await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html, replyToMessageId: messageId);
            return currentLength;
        }
    }
}
