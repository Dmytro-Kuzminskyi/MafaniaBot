using System;
using System.Linq;
using System.Threading;
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
                var tokenSource = new CancellationTokenSource();
                long chatId = message.Chat.Id;
                int userId = message.From.Id;
                int messageId = message.MessageId;
                string msg = null;

                if (message.Chat.Type != ChatType.Private)
                {
                    msg = $"Эта команда доступна только в <a href=\"{Startup.BOT_URL}\">личных сообщениях</a>!";
                    await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html, disableWebPagePreview: true, 
                        replyToMessageId: messageId);
                    return;
                }

                IDatabaseAsync db = redis.GetDatabase();
                //Check PendingAnswer from userId
                var pendingAnswerResponse = await db.StringGetAsync(new RedisKey($"PendingAnswer:{userId}"));

                if (!pendingAnswerResponse.IsNullOrEmpty)
                {
                    msg = "Сначала напишите ответ на вопрос!";
                    await botClient.SendTextMessageAsync(chatId, msg);
                    return;
                }
                //Check PendingQuestion from userId
                var pendingQuestionResponse = await db.HashExistsAsync(new RedisKey($"PendingQuestion:{userId}"), 
                        new RedisValue("Status"));

                if (pendingQuestionResponse)
                {
                    msg = "Сначала закончите с вопросом!";
                    await botClient.SendTextMessageAsync(chatId, msg);
                    return;
                }

                
                RedisValue[] recordset = await db.SetMembersAsync(new RedisKey("MyGroups"));
                var chatList = new List<long>(recordset.Select(e => long.Parse(e.ToString())));
                var tasks = chatList.Select(c =>
                    new {
                        ChatId = c,
                        Member = botClient.GetChatMemberAsync(c, userId)
                    });
                var chatsAvailable = new List<long>();

                foreach(var task in tasks)
                {  
                    try
                    {
                        long count = await db.SetLengthAsync(new RedisKey($"AskParticipants:{task.ChatId}"));
                        if (count != 0) {
                            if (count == 1 && await db.SetContainsAsync(new RedisKey($"AskParticipants:{task.ChatId}"),
                                new RedisValue(userId.ToString())))
							{
                                continue;
							}
                            ChatMember member = await task.Member;
                            if (member.Status == ChatMemberStatus.Creator || member.Status == ChatMemberStatus.Administrator ||
                                member.Status == ChatMemberStatus.Member)
                            {
                                chatsAvailable.Add(task.ChatId);
                            }
                        }
                    }
                    catch (ApiRequestException ex)
                    {
                        Logger.Log.Warn($"/ASK Not found #userId={userId} in #chatId={task.ChatId}", ex);
                    }
                }
            
                var tasksChatInfo = chatsAvailable.Select(chatId => botClient.GetChatAsync(chatId));
                Chat[] chats = await Task.WhenAll(tasksChatInfo);

                if (chats.Length == 0)
                {
                    msg = "Нет подходящих чатов, где можно задать анонимный вопрос!\n" +
                        "Вы можете добавить бота в группу";

                    var buttonAdd = InlineKeyboardButton.WithUrl("Добавить в группу", $"{Startup.BOT_URL}?startgroup=1");
                    var keyboardReg = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { buttonAdd } });
                    await botClient.SendTextMessageAsync(chatId, msg, replyMarkup: keyboardReg);
                    return;
                }
                var taskInitiate = db.HashSetAsync(new RedisKey($"PendingQuestion:{userId}"),
                        new[] { new HashEntry(new RedisValue("Status"), new RedisValue("Initiated")) });

                var keyboardData = new List<KeyValuePair<string, string>>();

                foreach(var chat in chats)
                {
                    keyboardData.Add(new KeyValuePair<string, string>(chat.Title, $"ask_select_chat&{chat.Id}"));
                }

                msg = "Выберите чат, участникам которого вы желаете задать анонимный вопрос";
                var keyboard = Helper.CreateInlineKeyboard(keyboardData, 1, "CallbackData").InlineKeyboard.ToList();
                var cancelBtn = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&") };
                keyboard.Add(cancelBtn);
                var token = tokenSource.Token;
                var messageTask = botClient.SendTextMessageAsync(chatId, msg, replyMarkup: new InlineKeyboardMarkup(keyboard),
                        cancellationToken: token);

                if (!taskInitiate.IsCompletedSuccessfully)
                {
                    tokenSource.Cancel();
                    await botClient.SendTextMessageAsync(chatId, "❌Ошибка сервера❌", ParseMode.Html);
                }

                await Task.WhenAll(new List<Task> { taskInitiate, messageTask });
                tokenSource.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log.Error("/ASK ---", ex);
            }
        }
    }
}