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
        private readonly GameEngine gameEngine;
        public ClassicWordsCommand()
        {
            gameEngine = GameEngine.Instance;
            Command = "/classicwords";
            Description = "Игра в классические слова";
        }

        public override bool Contains(Message message)
        {
            return message.Text.StartsWith(Command) || message.Text.StartsWith(Command + Startup.BOT_USERNAME);
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            Message message = update.Message;
            long chatId = message.Chat.Id;
            long userId = message.From.Id;
            string firstname = message.From.FirstName;
            int messageId = message.MessageId;
            DateTime messageDate = message.Date;
            string msg;

            if (message.Chat.Type == ChatType.Private)
            {
                await botClient.SendTextMessageAsync(chatId, "Эта команда доступна только в групповом чате.");
                return;
            }

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
