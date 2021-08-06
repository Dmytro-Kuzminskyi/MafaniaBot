using System;
using System.Threading.Tasks;
using MafaniaBot.Engines;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Commands
{
    public sealed class ClassicWordsCommand : Command
    {
        public ClassicWordsCommand()
        {
            Command = @"/classicwords";
            Description = "Классическая игра в слова";
        }

        public override bool Contains(Message message)
        {
            return (message.Text == Command ||
                message.Text == (Command + Startup.BOT_USERNAME)) &&
                message.Chat.Type != ChatType.Private;
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            var gameEngine = GameEngine.Instance;
            Message message = update.Message;
            long chatId = message.Chat.Id;
            long userId = message.From.Id;
            string firstname = message.From.FirstName;
            int messageId = message.MessageId;
            DateTime messageDate = message.Date;
            string msg;

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                if (!await db.SetContainsAsync(new RedisKey("MyChatMembers"), new RedisValue(userId.ToString())))
                {
                    msg = $"Нажми START в <a href=\"{Startup.BOT_URL + "?start=&activate"}\">личных сообщениях</a> чтобы играть в игры.";
                    await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html, replyToMessageId: messageId, disableWebPagePreview: true);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }

            if (gameEngine.FindGameByPlayerId(userId) != null)
            {
                msg = "Сначала закончи текущую игру!";
                await botClient.SendTextMessageAsync(chatId, msg, replyToMessageId: messageId);
                return;
            }

            var gameInvite = gameEngine.FindGameInviteFromUserByChatId(chatId, userId);

            if (gameInvite != null)
            {
                await gameEngine.RemoveGameInviteAsync(gameInvite);
            }
            
            await gameEngine.RegisterGameInviteAsync(new GameInvite(typeof(ClassicWordsGame), chatId, userId, firstname, messageDate, TimeSpan.FromMinutes(1)));
        }
    }
}
