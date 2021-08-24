using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Extensions;
using MafaniaBot.Helpers;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Commands
{
    public sealed class TitleCommand : ScopedCommand
    {
        public TitleCommand(BotCommandScopeType[] scopeTypes) : base(scopeTypes)
        {
            Command = "/title";
            Description = "Assign title to user";
        }

        public override bool Supported(Message message)
        {
            return message.Chat.Type != ChatType.Private &&
                    ((message.Text.Contains(Command) &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == Command.Length).Any()) ||
                    (message.Text.Contains($"{Command}@{Startup.BOT_USERNAME}") &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == $"{Command}@{Startup.BOT_USERNAME}".Length).Any()));
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis, ITranslateService translateService)
        {
            try
            {
                IDatabaseAsync db = redis.GetDatabase();
                Message message = update.Message;
                ChatType chatType = message.Chat.Type;
                long chatId = message.Chat.Id;
                int messageId = message.MessageId;
                var langCode = await db.HashGetAsync($"MyGroup:{chatId}", "LanguageCode");             

                var title = TextFormatter.GetTextWithoutCommand(message.Text, Command);
                title = string.Join(string.Empty, Regex.Split(title, "[^a-zA-Zа-яА-Яё\\s]+"));

                if (title.Length == 0)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"{translateService.GetResource("IncorrectCommandFormatString", langCode)}.",
                        replyToMessageId: messageId);

                    return;
                }

                var valueWithExpiry = await db.StringGetWithExpiryAsync($"LastTitle:{chatId}");

                if (!valueWithExpiry.Value.IsNull)
                {
                    var time = valueWithExpiry.Expiry;
                    var minutes = time?.Minutes;
                    var seconds = time?.Seconds;
                    var msg = $"{translateService.GetResource("NextTitleInString", langCode)}";

                    if (minutes == 0 && seconds == 0)
                    {
                        msg += $" 1 {translateService.GetResource("SecondsString", langCode)}";
                    }
                    else
                    {
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

                var chatMembersResult = (RedisKey[])await db.ExecuteAsync("KEYS", $"ChatMember:{chatId}:*");
                var randomUser = chatMembersResult.RandomElement();

                if (!long.TryParse(randomUser.ToString().Split(':').Last(), out var randomUserId))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"{translateService.GetResource("NoUserInfoString", langCode)}!");

                    return;
                }

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"{translateService.GetResource("FirstMessageString", langCode)} <b>{title}</b>...",
                    parseMode: ParseMode.Html);

                Thread.Sleep(TimeSpan.FromSeconds(1));

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"{translateService.GetResource("SecondMessageString", langCode)}!");

                Thread.Sleep(TimeSpan.FromSeconds(1));

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"{translateService.GetResource("ThirdMessageString", langCode)}!");

                Thread.Sleep(TimeSpan.FromSeconds(1));

                var member = (await botClient.GetChatMemberAsync(chatId, randomUserId)).User;
                var userMention = TextFormatter.GenerateMention(member.Id, member.FirstName, member.LastName);

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"{translateService.GetResource("TitleString", langCode)} <b>{title}</b> {translateService.GetResource("GetString", langCode)} {userMention}!",
                    parseMode: ParseMode.Html);

                var listLeftPushTask = db.ListLeftPushAsync($"Titles:{chatId}", $"{userMention} - {title}");
                var listTrimTask = db.ListTrimAsync($"Titles:{chatId}", 0, 9);
                var stringSetTask = db.StringSetAsync($"LastTitle:{chatId}", title, TimeSpan.FromHours(1));

                await Task.WhenAll(new Task[] { listLeftPushTask, listTrimTask, stringSetTask });

            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: error!", ex);
            }
        }
    }
}
