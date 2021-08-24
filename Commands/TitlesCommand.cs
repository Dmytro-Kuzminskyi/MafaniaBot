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
    public sealed class TitlesCommand : ScopedCommand
    {
        public TitlesCommand(BotCommandScopeType[] scopeTypes) : base(scopeTypes)
        {
            Command = "/titles";
            Description = "Chat titles";
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
                long chatId = message.Chat.Id;
                ChatType chatType = message.Chat.Type;
                string[] titleStrings = null;
                var langCode = await db.HashGetAsync($"MyGroup:{chatId}", "LanguageCode");

                titleStrings = (await db.ListRangeAsync($"Titles:{chatId}", 0, 9)).ToStringArray();

                var msg = $"<b>{translateService.GetResource("LastTitlesString", langCode)}</b>\n\n";
                var n = 1;

                foreach (var title in titleStrings)
                    msg += $"{n++}. {title}\n";

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: msg,
                    parseMode: ParseMode.Html);
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: error!", ex);
            }
        }
    }
}
