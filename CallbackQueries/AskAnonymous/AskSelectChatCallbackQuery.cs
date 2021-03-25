using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using StackExchange.Redis;
using Telegram.Bot.Exceptions;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
    public class AskSelectChatCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message.Chat.Type != ChatType.Private)
                return false;

            return callbackQuery.Data.StartsWith("ask_select_chat&");
        }

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            try
            {
                var tokenSource = new CancellationTokenSource();
                string data = callbackQuery.Data;
                long chatId = callbackQuery.Message.Chat.Id;
                int userId = callbackQuery.From.Id;
                int messageId = callbackQuery.Message.MessageId;
                long toChatId = long.Parse(data.Split('&')[1]);
                string msg = null;

                Logger.Log.Debug($"Initiated AskSelectChatCallback by #userId={callbackQuery.Message.Chat.Id} " +
                    $"with #data={callbackQuery.Data}");

                IDatabaseAsync db = redis.GetDatabase();
                RedisValue[] recordset = await db.SetMembersAsync(new RedisKey($"AskParticipants:{toChatId}"));
                var memberList = new List<int>(recordset.Select(e => int.Parse(e.ToString()))).Where(e => !e.Equals(userId));
                var userList = memberList.ToArray();

                if (userList.Length == 0)
                {
                    msg = "Некому задать анонимный вопрос, подожди пока кто-то подпишется!";
                    var task0 = botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg);
                    RedisValue[] records = await db.SetMembersAsync(new RedisKey("MyGroups"));
                    var chatList = new List<long>(records.Select(e => long.Parse(e.ToString())));
                    var tasksChatList = chatList.Select(c =>
                        new {
                            ChatId = c,
                            Member = botClient.GetChatMemberAsync(c, userId)
                        });
                    var chatsAvailable = new List<long>();

                    foreach (var task in tasksChatList)
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
                        var t0 = db.KeyDeleteAsync(new RedisKey($"PendingQuestion:{userId}"));
                        msg = "Нет подходящих чатов, где можно задать анонимный вопрос!\n" +
                            "Вы можете добавить бота в группу";

                        var buttonAdd = InlineKeyboardButton.WithUrl("Добавить в группу", $"{Startup.BOT_URL}?startgroup=1");
                        var keyboardReg = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { buttonAdd } });
                        var token = tokenSource.Token;
                        var t1 = botClient.EditMessageTextAsync(chatId, messageId, msg, replyMarkup: keyboardReg);

                        if (!t0.IsCompletedSuccessfully)
                        {
                            tokenSource.Cancel();
                            await botClient.SendTextMessageAsync(chatId, "❌Ошибка сервера❌", ParseMode.Html);
                        }

                        await Task.WhenAll(new List<Task> { t0, t1 });
                        tokenSource.Dispose();
                        return;
                    }

                    var kData = new List<KeyValuePair<string, string>>();

                    foreach (var chat in chats)
                    {
                        kData.Add(new KeyValuePair<string, string>(chat.Title, $"ask_select_chat&{chat.Id}"));
                    }

                    msg = "Выберите чат, участникам которого вы желаете задать анонимный вопрос";
                    var kBoard = Helper.CreateInlineKeyboard(kData, 1, "CallbackData").InlineKeyboard.ToList();
                    var cBtn = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&") };
                    kBoard.Add(cBtn);
                    var task1 = botClient.EditMessageTextAsync(chatId, messageId, msg, replyMarkup: new InlineKeyboardMarkup(kBoard));
                    await Task.WhenAll(new[] { task0, task1 });
                    return;
                }

                var dbTask = db.HashSetAsync(new RedisKey($"PendingQuestion:{userId}"),
                        new[] { new HashEntry("ChatId", toChatId.ToString()) });
                var tasks = userList.Where(id => id != userId).Select(id => botClient.GetChatMemberAsync(toChatId, id));
                var taskChat = botClient.GetChatAsync(toChatId);
                ChatMember[] chatMembers = await Task.WhenAll(tasks);
                var keyboardData = new List<KeyValuePair<string, string>>();

                foreach (var member in chatMembers)
                {
                    if (member.Status == ChatMemberStatus.Creator || member.Status == ChatMemberStatus.Administrator || 
                        member.Status == ChatMemberStatus.Member)
                    {
                        string firstname = member.User.FirstName;
                        string lastname = member.User.LastName;
                        int toUserId = member.User.Id;
                        string username = lastname != null ? firstname + " " + lastname : firstname;
                        keyboardData.Add(new KeyValuePair<string, string>(username, $"ask_select_user&{toChatId}:{toUserId}"));
                    }
                }

                var keyboard = Helper.CreateInlineKeyboard(keyboardData, 2, "CallbackData").InlineKeyboard.ToList();
                var cancelBtn = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&") };
                keyboard.Add(cancelBtn);
                string toChatTitle = (await taskChat).Title;
                msg = $"Выберите участника группы <b>{Helper.ConvertTextToHtmlParseMode(toChatTitle)}</b>," +
                    " которому вы желаете задать анонимный вопрос";                
                var messageTask = botClient.EditMessageTextAsync(chatId, messageId, msg, parseMode: ParseMode.Html, 
                        replyMarkup: new InlineKeyboardMarkup(keyboard));
                var t = new List<Task> { dbTask, messageTask };

                if (!dbTask.IsCompletedSuccessfully)
                {
                    tokenSource.Cancel();
                }

                await Task.WhenAll(t);
                tokenSource.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log.Error("AskSelectChatCallback ---", ex);
            }
        }
    }
}