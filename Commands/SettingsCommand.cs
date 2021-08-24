using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Handlers.CallbackQueryHandlers;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Commands
{
    public class SettingsCommand : ScopedCommand
    {
        public SettingsCommand(BotCommandScopeType[] scopeTypes) : base(scopeTypes)
        {
            Command = "/settings";
            Description = "Settings";
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
                ChatType chatType = message.Chat.Type;
                long chatId = message.Chat.Id;
                long userId = message.From.Id;
                int messageId = message.MessageId;
                var langCode  = chatType == ChatType.Private ? await db.HashGetAsync($"MyChatMember:{chatId}", "LanguageCode")
                                                            : await db.HashGetAsync($"MyGroup:{chatId}", "LanguageCode");

                if (chatType != ChatType.Private)
                {
                    var member = await botClient.GetChatMemberAsync(
                                    chatId: chatId,
                                    userId: userId);

                    if (member.Status != ChatMemberStatus.Creator)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"{translateService.GetResource("CreatorOnlyString", langCode)}.",
                            replyToMessageId: messageId);

                        return;
                    }
                }

                var langBtn = InlineKeyboardButton.WithCallbackData($"{translateService.GetResource("LanguageString", langCode)}", $"{LanguageCallbackQueryHandler.CallbackOperation}{userId}");
                var exitBtn = InlineKeyboardButton.WithCallbackData($"{translateService.GetResource("ExitString", langCode)}", $"{SettingsExitCallbackQueryHandler.CallbackOperation}{userId}");
                var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { langBtn } , new InlineKeyboardButton[] { exitBtn } });

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"{translateService.GetResource("MenuString", langCode)}",
                    replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name} error!", ex);
            }
        }
    }
}
