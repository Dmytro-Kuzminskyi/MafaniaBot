using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Helpers;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Commands
{
    public sealed class CallCommand : ScopedCommand
    {
        public CallCommand(BotCommandScopeType[] scopeTypes) : base(scopeTypes)
        {
            Command = "/call";
            Description = "Call chat members";
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
                var text = message.Text;
                var chatMemberIds = new List<long>();
                var isShortCommand = !text.Contains($"{Command}@{Startup.BOT_USERNAME}");
                var baseMsg = isShortCommand ? text.Replace(Command, string.Empty).Trim() + "\n"
                                            : text.Replace($"{Command}@{Startup.BOT_USERNAME}", string.Empty).Trim() + "\n";
                var msg = baseMsg;

                var langCode = await db.HashGetAsync($"MyGroup:{chatId}", "LanguageCode");

                var chatMembersResult = (RedisKey[])await db.ExecuteAsync("KEYS", $"ChatMember:{chatId}:*");

                foreach (var key in chatMembersResult)
                    chatMemberIds.Add(long.Parse(key.ToString().Split(':').Last()));

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"<b>{translateService.GetResource("GoHereString", langCode)}!</b>",
                    parseMode: ParseMode.Html);

                int i = 0;

                foreach (var userId in chatMemberIds)
                {
                    var icon = (await db.HashGetAsync($"ChatMember:{chatId}:{userId}", "CallIcon")).ToString();

                    msg += $"{TextFormatter.GenerateMention(userId, icon, null)}⠀";

                    if (i++ == 4)
                    {
                        msg = msg.Remove(msg.Length - 1);

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: msg,
                            parseMode: ParseMode.Html);

                        msg = baseMsg;
                        i = 0;
                    }
                }

                if (msg.Length > 0)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: msg,
                        parseMode: ParseMode.Html);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: error!", ex);
            }
        }
    }
}
