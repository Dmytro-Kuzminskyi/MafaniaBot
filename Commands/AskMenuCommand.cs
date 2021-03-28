using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using StackExchange.Redis;

namespace MafaniaBot.Commands
{
    public class AskMenuCommand : Command
    {
        public override string Pattern { get; }

        public override string Description { get; }

        public AskMenuCommand()
        {
            Pattern = @"/askmenu";
            Description = "Анонимные вопросы";
        }

        public override bool Contains(Message message)
        {
            return (message.Text.Equals(Pattern) || message.Text.Equals(Pattern + Startup.BOT_USERNAME)) && !message.From.IsBot;
        }

        public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            try
            {
                long chatId = message.Chat.Id;
                int userId = message.From.Id;
                string msg;

                Logger.Log.Info($"Initialized /ASKMENU #chatId={chatId} #userId={userId}");

                if (message.Chat.Type == ChatType.Channel || message.Chat.Type == ChatType.Private)
                {
                    msg = "Эта команда доступна только в групповом чате!";
                    await botClient.SendTextMessageAsync(chatId, msg);
                    return;
                }

                msg = "Меню анонимных вопросов";
                var registerBtn = InlineKeyboardButton.WithUrl("Зарегистрироваться", 
                    $"{Startup.BOT_URL}?start=ask_anon_register");
                var activateBtn = InlineKeyboardButton.WithCallbackData("Подписаться", "ask_activate&");
                var deactivateBtn = InlineKeyboardButton.WithCallbackData("Отписаться", "ask_deactivate&");
                var keyboard = new InlineKeyboardMarkup(new[] {
                    new InlineKeyboardButton[] { registerBtn },
                    new InlineKeyboardButton[] { activateBtn, deactivateBtn }
                });          
                await botClient.SendTextMessageAsync(chatId, msg, replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("/ASKMENU ---", ex);
            }
        }
    }
}