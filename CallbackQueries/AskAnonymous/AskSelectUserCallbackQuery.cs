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

		public override async Task Execute(CallbackQuery callbackQuery, 
			ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			try
			{
				long chatId = callbackQuery.Message.Chat.Id;
				string recipientData = callbackQuery.Data.Split('&')[1];
				long toChatId = long.Parse(recipientData.Split(':')[0]);
				int toUserId = int.Parse(recipientData.Split(':')[1]);
				string msg;

				Logger.Log.Debug($"Initiated AskSelectUserCallback by #userId={callbackQuery.Message.Chat.Id} with #data={callbackQuery.Data}");

				IDatabaseAsync db = redis.GetDatabase();
				bool isBotChatMember = await db.SetContainsAsync(new RedisKey("MyGroups"), new RedisValue(toChatId.ToString()));

				if (!isBotChatMember)
				{
					HandleBotIsNotChatMember(callbackQuery, db, botClient);
					return;
				}

				bool isUserSubscribed = await db.SetContainsAsync(new RedisKey($"AskParticipants:{toChatId}"), 
					new RedisValue(toUserId.ToString()));			

				if (!isUserSubscribed)
				{
					msg = "Этот пользователь отписался от анонимных вопросов!\n";
					RedisValue[] chatMembers = await db.SetMembersAsync(new RedisKey($"AskParticipants:{toChatId}"));
					var userList = chatMembers.Select(e => int.Parse(e.ToString()))
											.Where(id => id != callbackQuery.From.Id).ToArray();

					if (userList.Length == 0)
					{
						Chat[] chats = await GetChatsAvailableInfo(callbackQuery, db, botClient);

						if (chats.Length == 0)
						{
							HandleNoChatsAvailable(callbackQuery, db, botClient, msg);
							return;
						}

						HandleNoUsersAvailable(callbackQuery, botClient, chats, msg);
						return;
					}

					ProcessSelectUser(callbackQuery, botClient, userList, toChatId, toUserId, msg);
					return;
				}
				
				Process(callbackQuery, db, botClient, toChatId, toUserId);		
			}
			catch (Exception ex)
			{
				Logger.Log.Error("AskSelectUserCallback ---", ex);
			}
		}

		private async void Process(CallbackQuery callbackQuery, IDatabaseAsync db, ITelegramBotClient botClient,
			long toChatId, int toUserId)
		{
			long chatId = callbackQuery.Message.Chat.Id;
			int userId = callbackQuery.From.Id;
			int messageId = callbackQuery.Message.MessageId;
			ChatMember member = await GetChatMemberInfo(db, botClient, toChatId, toUserId);

			if (member.Status == ChatMemberStatus.Creator || 
				member.Status == ChatMemberStatus.Administrator || 
				member.Status == ChatMemberStatus.Member)
			{
				string firstname = member.User.FirstName;
				string lastname = member.User.LastName;
				string mention = Helper.GenerateMention(toUserId, firstname, lastname);
				var chatInfoTask = botClient.GetChatAsync(toChatId);
				var dbTask = db.HashSetAsync(new RedisKey($"PendingQuestion:{userId}"),
						new[] { new HashEntry("ToUserId", toUserId), 
								new HashEntry("MessageId", messageId.ToString()) 
						});			
				var cancelBtn = InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&");
				var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { cancelBtn } });
				var toChatTitle = (await chatInfoTask).Title;
				var msg = $"Напишите анонимный вопрос для {mention} из чата " +
					$"<b>{Helper.ConvertTextToHtmlParseMode(toChatTitle)}</b>";
				await dbTask;
				await botClient.EditMessageTextAsync(chatId, messageId, msg, ParseMode.Html, replyMarkup: keyboard);
			}
		}

		private async void ProcessSelectUser(CallbackQuery callbackQuery, ITelegramBotClient botClient,
			int[] userList, long toChatId, int toUserId, string msg)
		{
			long chatId = callbackQuery.Message.Chat.Id;
			int userId = callbackQuery.From.Id;
			int messageId = callbackQuery.Message.MessageId;
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
			msg += $"Выберите участника группы <b>{Helper.ConvertTextToHtmlParseMode(toChatTitle)}</b>, " +
				$"которому вы желаете задать анонимный вопрос";
			await botClient.EditMessageTextAsync(chatId, messageId, msg, parseMode: ParseMode.Html,
				replyMarkup: new InlineKeyboardMarkup(keyboard));
		}

		private async void HandleNoChatsAvailable(CallbackQuery callbackQuery, IDatabaseAsync db, ITelegramBotClient botClient,
			string msg)
		{
			long chatId = callbackQuery.Message.Chat.Id;
			int userId = callbackQuery.From.Id;
			int messageId = callbackQuery.Message.MessageId;
			var dbTask = db.KeyDeleteAsync(new RedisKey($"PendingQuestion:{userId}"));
			msg += "Нет подходящих чатов, где можно задать анонимный вопрос!\n" +
				"Вы можете добавить бота в группу";
			var addBtn = InlineKeyboardButton.WithUrl("Добавить в группу", $"{Startup.BOT_URL}?startgroup=1");
			var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { addBtn } });
			await dbTask;
			await botClient.EditMessageTextAsync(chatId, messageId, msg, replyMarkup: keyboard);
		}

		private async void HandleNoUsersAvailable(CallbackQuery callbackQuery, ITelegramBotClient botClient, 
			Chat[] chats, string msg)
		{
			long chatId = callbackQuery.Message.Chat.Id;
			int messageId = callbackQuery.Message.MessageId;
			var keyboardData = new List<KeyValuePair<string, string>>();
			msg += "В этом чате никто не подписан на анонимные вопросы!\n" +
				"Выберите чат, участникам которого вы желаете задать анонимный вопрос";

			foreach (var chat in chats)
			{
				keyboardData.Add(new KeyValuePair<string, string>(chat.Title, $"ask_select_chat&{chat.Id}"));
			}
	
			var keyboard = Helper.CreateInlineKeyboard(keyboardData, 1, "CallbackData").InlineKeyboard.ToList();
			var cancelBtn = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&") };
			keyboard.Add(cancelBtn);
			await botClient.EditMessageTextAsync(chatId, messageId, msg, replyMarkup: new InlineKeyboardMarkup(keyboard));
		}

		private async void HandleBotIsNotChatMember(CallbackQuery callbackQuery, 
			IDatabaseAsync db, ITelegramBotClient botClient)
		{
			long chatId = callbackQuery.Message.Chat.Id;
			int messageId = callbackQuery.Message.MessageId;
			Chat[] chats = await GetChatsAvailableInfo(callbackQuery, db, botClient);
			var keyboardData = new List<KeyValuePair<string, string>>();

			foreach (var chat in chats)
			{
				keyboardData.Add(new KeyValuePair<string, string>(chat.Title, $"ask_select_chat&{chat.Id}"));
			}

			var msg = "Бот удален из чата, невозможно задать вопрос!\n" +
				"Выберите чат, участникам которого вы желаете задать анонимный вопрос";
			var keyboard = Helper.CreateInlineKeyboard(keyboardData, 1, "CallbackData").InlineKeyboard.ToList();
			var cancelBtn = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена", "ask_cancel&") };
			keyboard.Add(cancelBtn);
			await botClient.EditMessageTextAsync(chatId, messageId, msg, replyMarkup: new InlineKeyboardMarkup(keyboard));
		}

		private async Task<Chat[]> GetChatsAvailableInfo(CallbackQuery callbackQuery, 
			IDatabaseAsync db, ITelegramBotClient botClient)
		{
			int userId = callbackQuery.From.Id;
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
			long toChatId, int toUserId)
		{
			var dbTask = db.SetContainsAsync(new RedisKey($"AskParticipants:{toChatId}"), new RedisValue(toUserId.ToString()));
			return (await dbTask) ? await botClient.GetChatMemberAsync(toChatId, toUserId) : null;
		}
	}
}