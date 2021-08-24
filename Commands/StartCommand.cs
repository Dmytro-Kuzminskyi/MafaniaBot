using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Commands
{
    public sealed class StartCommand : ScopedCommand
    {
        public StartCommand(BotCommandScopeType[] scopeTypes) : base(scopeTypes)
        {
            Command = "/start";
            Description = "Start";
        }

        public override bool Supported(Message message)
        {
            return message.Chat.Type == ChatType.Private &&
                    (((message.Text.Contains(Command) ||
                    message.Text.Contains($"{Command} &activate")) &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == Command.Length).Any()) ||
                    ((message.Text.Contains($"{Command}@{Startup.BOT_USERNAME}") ||
                    message.Text.Contains($"{Command}@{Startup.BOT_USERNAME} &activate")) &&
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
                string firstname = message.From.FirstName;
                var langCode = await db.HashGetAsync($"MyChatMember:{chatId}", "LanguageCode");

                if (message.Text.Contains($"{Command} &activate") || message.Text.Contains($"{Command}@{Startup.BOT_USERNAME} &activate"))
                {
                    var msg = $"{translateService.GetResource("HelloString", langCode)}, {firstname}!\n" +
                        $"{translateService.GetResource("PlayGamesString", langCode)}!\n" +
                        $"{translateService.GetResource("HelpString", langCode)}.";

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: msg);
                }
                else
                {
                    var msg = $"{translateService.GetResource("HelloString", langCode)}, {firstname}!\n" +
                        $"{translateService.GetResource("HelpString", langCode)}.\n" +
                        $"👇{translateService.GetResource("PlayTogetherString", langCode)}👇";

                    var addBtn = InlineKeyboardButton.WithUrl($"{translateService.GetResource("AddToGroupString", langCode)}", Startup.BOT_URL + $"?startgroup={userId}&invite");
                    var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { addBtn } });

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: msg,
                        replyMarkup: keyboard);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: error!", ex);
            }
        }
    }
}
