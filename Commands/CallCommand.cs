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
    public sealed class CallCommand : Command
    {
        public CallCommand()
        {
            Command = "/call";
            Description = "Призыв участников чата";
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
            var text = message.Text;
            var isShortCommand = !text.Contains($"{Command}@{Startup.BOT_USERNAME}");
            var baseMsg = isShortCommand ? text.Replace(Command, string.Empty).Trim() + "\n"
                                        : text.Replace($"{Command}@{Startup.BOT_USERNAME}", string.Empty).Trim() + "\n";
            var msg = baseMsg;

            if (message.Chat.Type == ChatType.Private)
            {
                await botClient.SendTextMessageAsync(chatId, "Эта команда доступна только в групповом чате.");
                return;
            }

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                var chatMembers = await db.SetMembersAsync(new RedisKey($"ChatMembers:{chatId}"));

                await botClient.SendTextMessageAsync(chatId, "<b>Все сюда!</b>", parseMode: ParseMode.Html);

                int i = 0;

                foreach(var chatMember in chatMembers)
                {
                    var icon = (await db.HashGetAsync(new RedisKey($"CallUserIcons:{chatId}"), new RedisValue(chatMember))).ToString();

                    msg += $"{TextFormatter.GenerateMention(long.Parse(chatMember.ToString()), icon, null)}⠀";

                    if (i++ == 4)
                    {
                        msg = msg.Remove(msg.Length - 1);
                        await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);
                        msg = baseMsg;
                        i = 0;
                    }
                }

                if (msg.Length > 0)
                {
                    await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }
        }
    }
}
