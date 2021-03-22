using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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

        public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer cache)
        {
            try
            {
                long chatId = message.Chat.Id;
                int messageId = message.MessageId;
                string msg = null;

                if (message.Chat.Type == ChatType.Channel || message.Chat.Type == ChatType.Private)
                {
                    msg = "Эта команда доступна только в групповом чате!";

                    Logger.Log.Debug($"/ASKMENU SendTextMessage #chatId={chatId} #msg={msg}");

                    await botClient.SendTextMessageAsync(chatId, msg);

                    return;
                }

                try
                {
                    Logger.Log.Debug($"/ASKMENU DeleteMessage #chatId={chatId} #messageId={messageId}");

                    await botClient.DeleteMessageAsync(chatId, messageId);
                }

                catch (ApiRequestException apiEx)
                {
                    if (apiEx.ErrorCode == 400)
                    {
                        Logger.Log.Warn("/ASKMENU Bad request: message can't be deleted");

                        msg = "Мне нужно право на удаление сообщений чтобы выполнить эту команду!";

                        Logger.Log.Debug($"/ASKMENU SendTextMessage #chatId={chatId} #msg={msg}");

                        await botClient.SendTextMessageAsync(chatId, msg);

                        return;
                    }
                }

                var buttonReg = InlineKeyboardButton.WithUrl("Зарегистрироваться", Startup.BOT_URL + "?start=ask_anon_register");
                var buttonActivate = InlineKeyboardButton.WithCallbackData("Подписаться", "&ask_anon_activate&");
                var buttonDeactivate = InlineKeyboardButton.WithCallbackData("Отписаться", "&ask_anon_deactivate&");
                var buttonAskAnon = InlineKeyboardButton.WithCallbackData("Задать анонимный вопрос", "&ask_anon_question&");

                var keyboard = new InlineKeyboardMarkup(new[] {
                    new InlineKeyboardButton[] { buttonReg },
                    new InlineKeyboardButton[] { buttonActivate, buttonDeactivate },
                    new InlineKeyboardButton[] { buttonAskAnon }
                });

                msg = "Меню анонимных вопросов";

                Logger.Log.Debug($"/ASKMENU SendTextMessage #chatId={chatId} #msg={msg}");

                await botClient.SendTextMessageAsync(chatId, msg, replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("/ASKMENU ---", ex);
            }
        }
    }
}