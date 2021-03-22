using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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

        public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer cache)
        {
            try
            {
                long chatId = message.Chat.Id;
                int messageId = message.MessageId;

                string msg =
                    "<b>Общие команды</b>\n" +
                    "/weather [city] — узнать текущую погоду.\n\n" +
                    "<b>Команды группового чата</b>\n" +
                    "/askmenu — меню анонимных вопросов.";

                Logger.Log.Debug($"/START SendTextMessage #chatId={chatId} #msg={msg}");

                await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html, replyToMessageId: messageId);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("/HELP ---", ex);
            }
        }
    }
}