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

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
	public class AskSelectUserCallbackQuery : Entity<CallbackQuery>
	{
		public override bool Contains(CallbackQuery callbackQuery)
		{
			if (callbackQuery.Message.Chat.Type != ChatType.Private)
				return false;

			return callbackQuery.Data.StartsWith("ask_select_user&");
		}

		public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			try
			{
				string data = callbackQuery.Data;
				long chatId = callbackQuery.Message.Chat.Id;
				int userId = callbackQuery.From.Id;
				int messageId = callbackQuery.Message.MessageId;
				string recipientData = data.Split('&')[1];
				long toChatId = long.Parse(recipientData.Split(':')[0]);
				int toUserId = int.Parse(recipientData.Split(':')[1]);
				string msg;

				Logger.Log.Debug($"Initiated AskSelectUserCallback by #userId={callbackQuery.Message.Chat.Id} with #data={callbackQuery.Data}");

				IDatabaseAsync db = redis.GetDatabase();
				bool isChatMember = await db.SetContainsAsync(new RedisKey("MyGroups"), new RedisValue(toChatId.ToString()));

				if (!isChatMember)
				{
					HandleBotIsNotChatMember(db, botClient, callbackQuery, chatId, userId, messageId);
					return;
				}

				ChatMember member = await GetChatMemberInfo(db, botClient, chatId, toChatId, toUserId);

				if (member != null)
				{
					Process(db, botClient, chatId, userId, messageId, member, toChatId, toUserId);
					return;
				}
		
				msg = "Этот пользователь покинул чат!";
				await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg);
				RedisValue[] chatMembers = await db.SetMembersAsync(new RedisKey($"AskParticipants:{toChatId}"));
				var userList = chatMembers.Select(e => int.Parse(e.ToString())).Where(id => id != userId).ToArray();

				if (userList.Length == 0)
				{					
					Chat[] chats = await GetChatsAvailableInfo(db, botClient, userId);

					if (chats.Length == 0)
					{
						HandleNoChatsAvailable(db, botClient, chatId, userId, messageId);
						return;
					}

					HandleNoUsersAvailable(botClient, callbackQuery, chatId, messageId, chats);
					return;
				}

				ProcessSelectUser(botClient, chatId, userId, messageId, userList, toChatId, toUserId);
			}
			catch (Exception ex)
			{
				Logger.Log.Error("AskSelectUserCallback ---", ex);
			}
		}

		private async void Process(IDatabaseAsync db, ITelegramBotClient botClient,
			long chatId, int userId, int messageId, ChatMember member, long toChatId, int toUserId)
		{
			if (member.Status == ChatMemberStatus.Creator || member.Status == ChatMemberStatus.Administrator || member.Status == ChatMemberStatus.Member)
			{
				var tokenSource = new CancellationTokenSource();
				var token = tokenSource.Token;
				string firstname = member.User.FirstName;
				string lastname = member.User.LastName;
				string mention = Helper.GenerateMention(toUserId, firstname, lastname);
				var chatInfoTask = botClient.GetChatAsync(toChatId);
				var dbTask = db.HashSetAsync(new RedisKey($"PendingQuestion:{userId}"),
						new[] { new HashEntry("ToUserId", toUserId) });			
				var cancelBtn = InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&");
				var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { cancelBtn } });
				var toChatTitle = (await chatInfoTask).Title;
				var msg = $"Напишите анонимный вопрос для {mention} из чата " +
					$"<b>{Helper.ConvertTextToHtmlParseMode(toChatTitle)}</b>";
				var messageTask = botClient.EditMessageTextAsync(chatId, messageId, msg, ParseMode.Html, replyMarkup: keyboard, cancellationToken: token);

				if (!dbTask.IsCompletedSuccessfully)
				{
					tokenSource.Cancel();
					await botClient.SendTextMessageAsync(chatId, "❌Ошибка сервера❌", ParseMode.Html);
				}

				await Task.WhenAll(new List<Task> { dbTask, messageTask });
				tokenSource.Dispose();
			}
		}

		private async void ProcessSelectUser(ITelegramBotClient botClient,
			long chatId, int userId, int messageId, int[] userList, long toChatId, int toUserId)
		{
			var chatInfoTask = botClient.GetChatAsync(toChatId);
			var tasks = userList.Where(id => id != userId).Select(id => botClient.GetChatMemberAsync(toChatId, id));
			ChatMember[] cMembers = await Task.WhenAll(tasks);
			var keyboardData = new List<KeyValuePair<string, string>>();

			foreach (var chatMember in cMembers)
			{
				if (chatMember.Status == ChatMemberStatus.Creator ||
					chatMember.Status == ChatMemberStatus.Administrator ||
					chatMember.Status == ChatMemberStatus.Member)
				{
					string firstname = chatMember.User.FirstName;
					string lastname = chatMember.User.LastName;
					toUserId = chatMember.User.Id;
					string username = lastname != null ? firstname + " " + lastname : firstname;
					keyboardData.Add(new KeyValuePair<string, string>(username, $"ask_select_user&{toChatId}:{toUserId}"));
				}
			}			
			var keyboard = Helper.CreateInlineKeyboard(keyboardData, 2, "CallbackData").InlineKeyboard.ToList();
			var cancelBtn = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&") };
			keyboard.Add(cancelBtn);
			var toChatTitle = (await chatInfoTask).Title;
			var msg = $"Выберите участника группы <b>{Helper.ConvertTextToHtmlParseMode(toChatTitle)}</b>, " +
				$"которому вы желаете задать анонимный вопрос";
			await botClient.EditMessageTextAsync(chatId, messageId, msg, parseMode: ParseMode.Html,
				replyMarkup: new InlineKeyboardMarkup(keyboard));
		}

		private async void HandleNoChatsAvailable(IDatabaseAsync db, ITelegramBotClient botClient,
			long chatId, int userId, int messageId)
		{
			var tokenSource = new CancellationTokenSource();
			var token = tokenSource.Token;
			var dbTask = db.KeyDeleteAsync(new RedisKey($"PendingQuestion:{userId}"));
			var msg = "Нет подходящих чатов, где можно задать анонимный вопрос!\n" +
				"Вы можете добавить бота в группу";
			var buttonAdd = InlineKeyboardButton.WithUrl("Добавить в группу", $"{Startup.BOT_URL}?startgroup=1");
			var keyboardReg = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { buttonAdd } });
			var messageTask = botClient.EditMessageTextAsync(chatId, messageId, msg, replyMarkup: keyboardReg, cancellationToken: token);

			if (!dbTask.IsCompletedSuccessfully)
			{
				tokenSource.Cancel();
				await botClient.SendTextMessageAsync(chatId, "❌Ошибка сервера❌", ParseMode.Html);
			}

			await Task.WhenAll(new List<Task> { dbTask, messageTask });
			tokenSource.Dispose();
		}

		private async void HandleNoUsersAvailable(ITelegramBotClient botClient, CallbackQuery callbackQuery,
			long chatId, int messageId, Chat[] chats)
		{
			var keyboardData = new List<KeyValuePair<string, string>>();

			foreach (var chat in chats)
			{
				keyboardData.Add(new KeyValuePair<string, string>(chat.Title, $"ask_select_chat&{chat.Id}"));
			}

			var msg = "Некому задать анонимный вопрос, подожди пока кто-то подпишется!";
			var answerTask = botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg);
			msg = "Выберите чат, участникам которого вы желаете задать анонимный вопрос";
			var keyboard = Helper.CreateInlineKeyboard(keyboardData, 1, "CallbackData").InlineKeyboard.ToList();
			var cancelBtn = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&") };
			keyboard.Add(cancelBtn);
			var messageTask = botClient.EditMessageTextAsync(chatId, messageId, msg,
				replyMarkup: new InlineKeyboardMarkup(keyboard));
			await Task.WhenAll(new[] { answerTask, messageTask });
		}

		private async void HandleBotIsNotChatMember(IDatabaseAsync db, ITelegramBotClient botClient, CallbackQuery callbackQuery, long chatId, int userId, int messageId)
		{
			Chat[] chats = await GetChatsAvailableInfo(db, botClient, userId);
			var keyboardData = new List<KeyValuePair<string, string>>();

			foreach (var chat in chats)
			{
				keyboardData.Add(new KeyValuePair<string, string>(chat.Title, $"ask_select_chat&{chat.Id}"));
			}

			var msg = "Бот удален из чата, невозможно задать вопрос!";
			var answerTask = botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg);
			msg = "Выберите чат, участникам которого вы желаете задать анонимный вопрос";
			var keyboard = Helper.CreateInlineKeyboard(keyboardData, 1, "CallbackData").InlineKeyboard.ToList();
			var cancelBtn = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&") };
			keyboard.Add(cancelBtn);
			var messageTask = botClient.EditMessageTextAsync(chatId, messageId, msg,
				replyMarkup: new InlineKeyboardMarkup(keyboard));
			await Task.WhenAll(new[] { answerTask, messageTask });
		}

		private async Task<Chat[]> GetChatsAvailableInfo(IDatabaseAsync db, ITelegramBotClient botClient, int userId)
		{
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

		private async Task<ChatMember> GetChatMemberInfo(IDatabaseAsync db, ITelegramBotClient botClient, 
			long chatId, long toChatId, int toUserId)
		{
			var tokenSource = new CancellationTokenSource();
			var token = tokenSource.Token;
			var dbTask = db.SetContainsAsync(new RedisKey($"AskParticipants:{chatId}"), new RedisValue(toUserId.ToString()));
			var memberInfoTask = botClient.GetChatMemberAsync(toChatId, toUserId, cancellationToken: token);

			if (!dbTask.IsCompletedSuccessfully)
			{
				tokenSource.Cancel();
				await botClient.SendTextMessageAsync(chatId, "❌Ошибка сервера❌", ParseMode.Html);
			}

			if (await dbTask)
			{
				return await memberInfoTask;
			}

			return null;
		}
	}
}