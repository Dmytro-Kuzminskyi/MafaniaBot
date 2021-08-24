using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Dictionaries;
using MafaniaBot.Extensions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Commands
{
    public sealed class ChangeIconCommand : ScopedCommand
    {
        public ChangeIconCommand(BotCommandScopeType[] scopeTypes) : base(scopeTypes)
        {
            Command = "/changeicon";
            Description = "Change call icon";
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
                long userId = message.From.Id;
                int messageId = message.MessageId;
                string langCode = await db.HashGetAsync($"MyGroup:{chatId}", "LanguageCode");

                var icon = BaseDictionary.CallIcons.RandomElement();

                await db.HashSetAsync($"ChatMember:{chatId}:{userId}", new [] { new HashEntry("CallIcon", icon) });

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"{translateService.GetResource("NewIconString", langCode)}: {icon}",
                    parseMode: ParseMode.Html,
                    replyToMessageId: messageId);
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: error!", ex);
            }
        }
    }
}
