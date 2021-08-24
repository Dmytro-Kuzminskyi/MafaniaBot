using System;
using System.Collections.Generic;
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
    public class InformCommand : ScopedCommand
    {
        public InformCommand(BotCommandScopeType[] scopeTypes) : base(scopeTypes)
        {
            Command = "/inform";
            Description = "Inform groups";
        }

        public override bool Supported(Message message)
        {
            return message.Chat.Type == ChatType.Private &&
                    message.From.Id == Startup.SUPPORT_USERID &&
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
                var text = message.Text;
                var langCode = string.Empty;
                var isShortCommand = !text.Contains($"{Command}@{Startup.BOT_USERNAME}");
                text = isShortCommand ? text.Replace(Command, string.Empty).Trim() + "\n"
                                        : text.Replace($"{Command}@{Startup.BOT_USERNAME}", string.Empty).Trim() + "\n";

                foreach (var supportedLanguage in translateService.SupportedLanguages)
                {
                    if (text.Contains($"&{supportedLanguage}&"))
                    {
                        langCode = supportedLanguage;
                        text = text.Replace($"&{supportedLanguage}&", string.Empty).Trim();

                        break;
                    }
                }

                var myGroupsResult = (RedisKey[])await db.ExecuteAsync("KEYS", "MyGroup:*");
                var chats = new List<long>();

                foreach (var key in myGroupsResult)
                {
                    var chatId = long.Parse(key.ToString().Split(':').LastOrDefault());

                    var chatLanguageCode = await db.HashGetAsync($"MyGroup:{chatId}", "LanguageCode");

                    if (langCode == chatLanguageCode)
                    {
                        chats.Add(chatId);
                    }
                }

                Parallel.ForEach(chats, async chat => 
                {
                    var msg = $"<b>{translateService.GetResource("SystemMessageString", langCode)}</b>\n";
                    msg += text;

                    await botClient.SendTextMessageAsync(
                        chatId: chat,
                        text: msg,
                        parseMode: ParseMode.Html);
                });
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: error!", ex);
            }
        }
    }
}
