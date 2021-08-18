using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MafaniaBot.Helpers;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Commands
{
    public sealed class TitleCommand : Command
    {
        public TitleCommand()
        {
            Command = "/title";
            Description = "Дать звание участнику чата";
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
            int messageId = message.MessageId;
            string msg;

            if (message.Chat.Type == ChatType.Private)
            {
                await botClient.SendTextMessageAsync(chatId, "Эта команда доступна только в групповом чате.");
                return;
            }

            var title = TextFormatter.GetTextWithoutCommand(message.Text, Command);
            title = string.Join(string.Empty, Regex.Split(title, "[^a-zA-Zа-яА-Яё\\s]+"));

            if (title.Length == 0)
            {
                msg = "Введи команду в формате /title [звание].";

                await botClient.SendTextMessageAsync(chatId, msg, replyToMessageId: messageId);
                return;
            }

            try
            {
                IDatabaseAsync db = redis.GetDatabase();
                var valueWithExpiry = await db.StringGetWithExpiryAsync($"LastTitle:{chatId}");

                if (!valueWithExpiry.Value.IsNull)
                {
                    var time = valueWithExpiry.Expiry;
                    var minutes = time?.Minutes;
                    var seconds = time?.Seconds;
                    msg = $"Следующее звание можно будет выбрать через";

                    if (minutes == 0 && seconds == 0)
                    {
                        msg += " 1 сек";
                    }
                    else
                    {
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
            }
            catch(Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }

            Task<RedisValue> randomUserIdTask = null;

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                randomUserIdTask = db.SetRandomMemberAsync($"ChatMembers:{chatId}");
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }

            if (!long.TryParse((await randomUserIdTask).ToString(), out var randomUserId))
            {
                await botClient.SendTextMessageAsync(chatId, "Погодите немножко, собираю информацию об участниках чата!");
                return;
            }

            await botClient.SendTextMessageAsync(chatId, $"Ну-ка, сейчас посмотрим кто у нас <b>{title}</b>...", parseMode: ParseMode.Html);
            Thread.Sleep(TimeSpan.FromSeconds(1));
            await botClient.SendTextMessageAsync(chatId, "Ставим ставки господа!");
            Thread.Sleep(TimeSpan.FromSeconds(1));
            await botClient.SendTextMessageAsync(chatId, "Простите, если кто-то не успел, но пора обьявить победителя!");
            Thread.Sleep(TimeSpan.FromSeconds(1)); 

            var member = (await botClient.GetChatMemberAsync(chatId, randomUserId)).User;
            var userMention = TextFormatter.GenerateMention(member.Id, member.FirstName, member.LastName);

            msg = $"Итак, звание <b>{title}</b> получает {userMention}!";

            await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html);

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                var listLeftPushTask = db.ListLeftPushAsync($"Titles:{chatId}", $"{userMention} - {title}");
                var listTrimTask = db.ListTrimAsync($"Titles:{chatId}", 0, 9);
                var stringSetTask = db.StringSetAsync($"LastTitle:{chatId}", title, TimeSpan.FromHours(1));

                await Task.WhenAll(new Task[] { listLeftPushTask, listTrimTask, stringSetTask });
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }
        }
    }
}
