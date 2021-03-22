using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using StackExchange.Redis;

namespace MafaniaBot.Commands
{
    public class AskCommand : Command
    {
        public override string Pattern { get; }

        public override string Description { get; }

        public AskCommand()
        {
            Pattern = @"/ask";
            Description = "Задать анонимный вопрос";
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
                int messageId = message.MessageId;

                string msg = null;

                if (message.Chat.Type != ChatType.Private)
                {
                    msg = $"Эта команда доступна только в <a href=\"{Startup.BOT_URL}\">личных сообщениях</a>!";

                    Logger.Log.Debug($"/START SendTextMessage #chatId={chatId} #msg={msg}");

                    await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html, disableWebPagePreview: true, replyToMessageId: messageId);

                    return;
                }

                try
                {

                }
                catch (Exception ex)
                {
                    Logger.Log.Error("/START Error while processing Redis.MyChatMembers", ex);
                }



            }
            catch (Exception ex)
            {
                Logger.Log.Error("/START ---", ex);
            }
        }
    }
}
