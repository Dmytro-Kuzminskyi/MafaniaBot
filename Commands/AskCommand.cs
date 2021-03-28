using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
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
                Logger.Log.Info($"Initialized /ASK #chatId={message.Chat.Id} #userId={message.From.Id}");

                if (message.Chat.Type != ChatType.Private)
                {
                    string msg = $"Эта команда доступна только в <a href=\"{Startup.BOT_URL}\">личных сообщениях</a>!";
                    await botClient.SendTextMessageAsync(message.Chat.Id, msg, ParseMode.Html, disableWebPagePreview: true, 
                        replyToMessageId: message.MessageId);
                    return;
                }

                IDatabaseAsync db = redis.GetDatabase();
                var chatAvailableTask = GetChatsAvailableInfo(message, db, botClient);

                if (await HandlePendingAnswer(message, db, botClient))
                    return;

                if (await HandlePendingQuestion(message, db, botClient))
                    return;

                Chat[] chats = await chatAvailableTask;

                Process(message, db, botClient, chats);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("/ASK ---", ex);
            }
        }

        private async Task<bool> HandlePendingAnswer(Message message, IDatabaseAsync db, ITelegramBotClient botClient)
		{
            long chatId = message.Chat.Id;
            int userId = message.From.Id;
            bool state = false;
            bool value = await db.HashExistsAsync(new RedisKey($"PendingAnswer:{userId}"), new RedisValue("Status"));

            if (value)
            {
                string msg = "Сначала напишите ответ на вопрос!";
                await botClient.SendTextMessageAsync(chatId, msg);
                state = true;
            }

            return state;
        }

        private async Task<bool> HandlePendingQuestion(Message message, IDatabaseAsync db, ITelegramBotClient botClient)
		{
            long chatId = message.Chat.Id;
            int userId = message.From.Id;
            bool state = false;
            bool value = await db.HashExistsAsync(new RedisKey($"PendingQuestion:{userId}"), new RedisValue("Status"));

            if (value)
            {
                string msg = "Сначала закончите с вопросом!";
                await botClient.SendTextMessageAsync(chatId, msg);
                state = true;
            }

            return state;
        }

        private async Task<Chat[]> GetChatsAvailableInfo(Message message, IDatabaseAsync db, ITelegramBotClient botClient)
		{
            int userId = message.From.Id;
            RedisValue[] recordset = await db.SetMembersAsync(new RedisKey("MyGroups"));
            var chatList = new List<long>(recordset.Select(e => long.Parse(e.ToString())));
            var tasks = chatList.Select(c =>
                new {
                    ChatId = c,
                    Member = botClient.GetChatMemberAsync(c, userId)
                });
            chatList = new List<long>();

            foreach (var task in tasks)
            {
                try
                {
                    long count = await db.SetLengthAsync(new RedisKey($"AskParticipants:{task.ChatId}"));
                    if (count != 0)
                    {
                        if (count == 1 && await db.SetContainsAsync(new RedisKey($"AskParticipants:{task.ChatId}"),
                            new RedisValue(userId.ToString())))
                        {
                            continue;
                        }
                        ChatMember member = await task.Member;
                        if (member.Status == ChatMemberStatus.Creator || 
                            member.Status == ChatMemberStatus.Administrator ||
                            member.Status == ChatMemberStatus.Member)
                        {
                            chatList.Add(task.ChatId);
                        }
                    }
                }
                catch (ApiRequestException ex)
                {
                    Logger.Log.Warn($"/ASK Not found #userId={userId} in #chatId={task.ChatId}", ex);
                }
            }

            var tasksChatInfo = chatList.Select(chatId => botClient.GetChatAsync(chatId));
            Chat[] chats = await Task.WhenAll(tasksChatInfo);
            return chats;
        }

        private async void Process(Message message, IDatabaseAsync db, ITelegramBotClient botClient, Chat[] chats)
		{
            long chatId = message.Chat.Id;
            int userId = message.From.Id;
            string msg;

			if (chats.Length == 0)
            {
                msg = "Нет подходящих чатов, где можно задать анонимный вопрос!\n" +
                    "Вы можете добавить бота в группу";

                var addBtn = InlineKeyboardButton.WithUrl("Добавить в группу", $"{Startup.BOT_URL}?startgroup=1");
                var keyboardReg = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { addBtn } });
                await botClient.SendTextMessageAsync(chatId, msg, replyMarkup: keyboardReg);
                return;
            }

            var dbTask = db.HashSetAsync(new RedisKey($"PendingQuestion:{userId}"),
                    new[] { new HashEntry(new RedisValue("Status"), new RedisValue("Initiated")) });
            var keyboardData = new List<KeyValuePair<string, string>>();

            foreach (var chat in chats)
            {
                keyboardData.Add(new KeyValuePair<string, string>(chat.Title, $"ask_select_chat&{chat.Id}"));
            }    
            msg = "Выберите чат, участникам которого вы желаете задать анонимный вопрос";
            var keyboard = Helper.CreateInlineKeyboard(keyboardData, 1, "CallbackData").InlineKeyboard.ToList();
            var cancelBtn = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&") };
            keyboard.Add(cancelBtn);
            await dbTask;
            await botClient.SendTextMessageAsync(chatId, msg, replyMarkup: new InlineKeyboardMarkup(keyboard));
        }
    }
}