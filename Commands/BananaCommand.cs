using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Commands
{
    public sealed class BananaCommand : ScopedCommand
    {
        public BananaCommand(BotCommandScopeType[] scopeTypes) : base(scopeTypes)
        {
            Command = "/banana";
            Description = "Grow banana";
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
                long userId = message.From.Id;
                int messageId = message.MessageId;
                User userInfo = null;
                string positionDiffIcon = string.Empty;
                string positionText;
                string msg;

                var langCode = message.Chat.Type == ChatType.Private ? await db.HashGetAsync($"MyChatMember:{chatId}", "LanguageCode")
                                                                : await db.HashGetAsync($"MyGroup:{chatId}", "LanguageCode");

                userInfo = (await botClient.GetChatMemberAsync(
                    chatId: chatId,
                    userId: userId)).User;

                var valueWithExpiryTask = db.StringGetWithExpiryAsync($"LastBanana:{userId}");
                var userName = $"{userInfo.FirstName} {userInfo.LastName}".Trim();

                await db.HashSetAsync($"Banana:{userId}", new[] { new HashEntry("Name", userName) });

                var valueWithExpiry = await valueWithExpiryTask;

                if (!valueWithExpiry.Value.IsNull)
                {
                    var time = valueWithExpiry.Expiry;
                    var hours = time?.Hours;
                    var minutes = time?.Minutes;
                    var seconds = time?.Seconds;
                    msg = $"{translateService.GetResource("NextTryInString", langCode)}";

                    if (hours == 0 && minutes == 0 && seconds == 0)
                    {
                        msg += $" 1 {translateService.GetResource("SecondsString", langCode)}";
                    }
                    else
                    {
                        if (hours > 0)
                        {
                            msg += $" {hours} {translateService.GetResource("HoursString", langCode)}";
                        }

                        if (minutes > 0)
                        {
                            msg += $" {minutes} {translateService.GetResource("MinutesString", langCode)}";
                        }

                        if (seconds > 0)
                        {
                            msg += $" {seconds} {translateService.GetResource("SecondsString", langCode)}";
                        }
                    }

                    msg += "!";

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: msg,
                        replyToMessageId: messageId);

                    return;
                }

                var userData = await db.HashGetAllAsync($"Banana:{userId}");

                float.TryParse(userData?.Where(e => e.Name == "Length").Select(e => e.Value).FirstOrDefault(), out var length);
                length = BananaGrowHandler(length, out var dirrefence, out var isGrow);

                msg = isGrow ? $"{translateService.GetResource("YourString", langCode)} 🍌 {translateService.GetResource("GrewUpString", langCode)} {string.Format("{0:0.00}", dirrefence)} {translateService.GetResource("CentimetersString", langCode)}!\n"
                            : $"{translateService.GetResource("YourString", langCode)} 🍌 {translateService.GetResource("ShrunkString", langCode)} {string.Format("{0:0.00}", dirrefence)} {translateService.GetResource("CentimetersString", langCode)}!\n";

                msg += $"{translateService.GetResource("NewLengthString", langCode)} {string.Format("{0:0.00}", length)} {translateService.GetResource("CentimetersString", langCode)}!\n";

                var currentPosition = (await db.SortedSetRankAsync($"TopBananas", userId, Order.Descending) + 1);

                var hashSetTask = db.HashSetAsync($"Banana:{userId}", new[] { new HashEntry("Length", length.ToString("n2")) });
                var sortedSetAddTask = db.SortedSetAddAsync($"TopBananas", userId, length);
                var stringSetTask = db.StringSetAsync($"LastBanana:{userId}", string.Empty, TimeSpan.FromHours(4));

                await Task.WhenAll(new Task[] { hashSetTask, sortedSetAddTask, stringSetTask });

                var nextPosition = (await db.SortedSetRankAsync($"TopBananas", userId, Order.Descending) + 1);

                var positionDiff = (currentPosition ?? 0) - (nextPosition ?? 0);

                if (positionDiff > 0)
                    positionDiffIcon = "🔼";
                else if (positionDiff < 0)
                    positionDiffIcon = "🔽";

                positionText = nextPosition.ToString();

                switch (nextPosition)
                {
                    case 1:
                        positionText = "🥇";
                        break;
                    case 2:
                        positionText = "🥈";
                        break;
                    case 3:
                        positionText = "🥉";
                        break;
                }

                var positionDiffText = positionDiff != 0 ? Math.Abs(positionDiff).ToString() : string.Empty;

                msg += $"{translateService.GetResource("GlobalTopPlaceString", langCode)}: <b>{positionText}</b>   {positionDiffIcon} <b>{positionDiffText}</b>";

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: msg,
                    parseMode: ParseMode.Html,
                    replyToMessageId: messageId);
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: error!", ex);
            }
        }

        private float BananaGrowHandler(float? length, out float difference, out bool isGrow)
        {
            var random = new Random(DateTime.Now.Millisecond);
            var currentLength = length ?? 0;

            float lengthDiff = 50 - currentLength;
            float chanceToGrowInitial = 0.6f;
            float chanceToGrowStep = 0.008f;
            float growMultiplierInitial = 0.2f;
            float growMultiplierStep = 0.04f;

            float totalChanceToGrow = lengthDiff > 0 ? (lengthDiff * chanceToGrowStep) + chanceToGrowInitial : chanceToGrowInitial;
            float totalGrowMultiplier = lengthDiff > 0 ? (lengthDiff * growMultiplierStep) + growMultiplierInitial : growMultiplierInitial;

            difference = (float)(random.NextDouble() * totalGrowMultiplier);

            if (random.NextDouble() <= totalChanceToGrow)
            {
                currentLength += difference;
                isGrow = true;
            }
            else
            {
                currentLength -= difference;
                currentLength = currentLength < 0 ? 0 : currentLength;
                isGrow = false;
            }

            return currentLength;
        }
    }
}
