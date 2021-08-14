using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Dictionaries;
using MafaniaBot.Extensions;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Commands
{
    public sealed class ChangeIconCommand : Command
    {
        public ChangeIconCommand()
        {
            Command = "/changeicon";
            Description = "Изменить свою иконку призыва";
        }

        public override bool Contains(Message message)
        {
            return message.Text.Contains(Command) &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == Command.Length).Any() ||
                    message.Text.Contains($"{Command}@{Startup.BOT_USERNAME}") &&
                    message.Entities.Where(e => e.Offset == 0 && e.Length == $"{Command}@{Startup.BOT_USERNAME}".Length).Any();
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            Message message = update.Message;
            long chatId = message.Chat.Id;
            long userId = message.From.Id;
            int messageId = message.MessageId;

            if (message.Chat.Type == ChatType.Private)
            {
                await botClient.SendTextMessageAsync(chatId, "Эта команда доступна только в групповом чате.");
                return;
            }

            var icon = BaseDictionary.Icons.RandomElement();
            var hashEntry = new HashEntry(new RedisValue(userId.ToString()), new RedisValue(icon));

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                var hashSetTask = db.HashSetAsync(new RedisKey($"CallUserIcons:{chatId}"), new HashEntry[] { hashEntry });
                var stringSetTask = db.StringSetAsync(new RedisKey($"ActiveChatMembers:{chatId}:{userId}"), icon, TimeSpan.FromDays(1));

                await Task.WhenAll(new Task[] { hashSetTask, stringSetTask });
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }

            await botClient.SendTextMessageAsync(chatId, $"Твоя новая иконка призыва: {icon}", parseMode: ParseMode.Html, replyToMessageId: messageId);
        }
    }
}
