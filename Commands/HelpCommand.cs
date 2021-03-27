using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StackExchange.Redis;

namespace MafaniaBot.Commands
{
    public class HelpCommand : Command
    {
        public override string Pattern { get; }

        public override string Description { get;  }

        public HelpCommand()
        {
            Pattern = @"/help";
            Description = "Помощь по командам";
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
                int messageId = message.MessageId;

                string msg =
                    "<b>Общие команды</b>\n" +
                    "/weather [city] — узнать текущую погоду\n" +
                    "/help — справка по командам\n\n" +
                    "<b>Команды личного чата</b>\n" +
                    "/ask — задать анонимный вопрос\n\n" +
                    "<b>Команды группового чата</b>\n" +
                    "/askmenu — меню анонимных вопросов\n\n";
                await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html, replyToMessageId: messageId);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("/HELP ---", ex);
            }
        }
    }
}