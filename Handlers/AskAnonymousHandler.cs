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

namespace MafaniaBot.Handlers
{
	public class AskAnonymousHandler : Entity<Message>
	{
		public override bool Contains(Message message)
		{
			if (message.Chat.Type != ChatType.Private)
				return false;

			return !message.From.IsBot;
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			try
			{
				HashEntry[] record = null;

				IDatabaseAsync db = redis.GetDatabase();
				record = await db.HashGetAllAsync(new RedisKey($"PendingQuestion:{message.From.Id}"));

				if (record.Length != 0)
				{
					Logger.Log.Debug($"AskAnonymous HANDLER - pending question triggered in " +
						$"#chatId={message.Chat.Id} #userId={message.From.Id}");
					HandlePendingQuestion(message, db, botClient, record);
					return;
				}

				record = await db.HashGetAllAsync(new RedisKey($"PendingAnswer:{message.From.Id}"));

				if (record.Length != 0)
				{
					Logger.Log.Debug($"AskAnonymous HANDLER - pending answer triggered in " +
						$"#chatId={message.Chat.Id} #userId={message.From.Id}");
					HandlePendingAnswer(message, db, botClient, record);
				}
			}
			catch (Exception ex)
			{
				Logger.Log.Error($"AskAnonymous HANDLER ---", ex);
			}
		}

		private async void HandlePendingQuestion(Message message, IDatabaseAsync db, ITelegramBotClient botClient, 
			HashEntry[] record)
		{
			bool isBotBlocked = false;
			long chatId = message.Chat.Id;
			int userId = message.From.Id;
			string msg;

			if (message.Text == null)
			{
				msg = "Отправьте текстовое сообщение!";
				await botClient.SendTextMessageAsync(chatId, msg);
				return;
			}

			var pendingQuestion = record.ToDictionary();
			long toChatId = long.Parse(pendingQuestion["ChatId"].ToString());
			int toUserId = int.Parse(pendingQuestion["ToUserId"].ToString());
			int messageId = int.Parse(pendingQuestion["MessageId"].ToString());
			bool isBotChatMember = await db.SetContainsAsync(new RedisKey("MyGroups"), pendingQuestion["ChatId"]);

			if (!isBotChatMember)
			{
				HandleBotIsNotChatMember(message, db, botClient);
				return;
			}

			var chatInfoTask = botClient.GetChatAsync(toChatId);
			bool isUserSubscribed = await db.SetContainsAsync(new RedisKey($"AskParticipants:{toChatId}"),
				pendingQuestion["ToUserId"]);

			if (!isUserSubscribed)
			{
				msg = "Этот пользователь отписался от анонимных вопросов!\n";
				RedisValue[] chatMembers = await db.SetMembersAsync(new RedisKey($"AskParticipants:{toChatId}"));
				var userList = chatMembers.Select(e => int.Parse(e.ToString()))
										.Where(id => id != userId).ToArray();

				if (userList.Length == 0)
				{
					Chat[] chats = await GetChatsAvailableInfo(message, db, botClient);

					if (chats.Length == 0)
					{
						HandleNoChatsAvailable(message, db, botClient, msg);
						return;
					}

					HandleNoUsersAvailable(message, botClient, chats, msg);
					return;
				}

				ProcessSelectUser(message, botClient, userList, toChatId, toUserId, msg);
				return;
			}

			var toChatTitle = (await chatInfoTask).Title;
			var userInfoTask = botClient.GetChatMemberAsync(toChatId, toUserId);
			msg = $"Пользователь из чата <b>{Helper.ConvertTextToHtmlParseMode(toChatTitle)}</b> задал вам вопрос:\n" +
					$"\"{Helper.ConvertTextToHtmlParseMode(message.Text)}\"";
			var answerBtn = InlineKeyboardButton.WithCallbackData("Ответить", $"answer&{userId}");
			var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[] { answerBtn });
			try
			{
				await botClient.SendTextMessageAsync(toUserId, msg, ParseMode.Html, replyMarkup: keyboard);
			}
			catch (ApiRequestException apiEx)
			{
				if (apiEx.ErrorCode == 403)
				{
					isBotBlocked = true;
					Logger.Log.Warn($"AskAnonymous HANDLER Forbidden: bot was blocked by the user - #userId={toUserId}");
					var dbTask0 = db.SetRemoveAsync(new RedisKey("MyChatMembers"), pendingQuestion["ToUserId"]);
					var dbTask1 = db.SetRemoveAsync(new RedisKey($"AskParticipants:{toChatId}"), pendingQuestion["ToUserId"]);
					var dbTask2 = db.KeyDeleteAsync(new RedisKey($"PendingQuestion:{userId}"));
					await Task.WhenAll(new[] { dbTask0, dbTask1, dbTask2 });
					msg = "Сообщение не отправлено, пользователь заблокировал бота!";
					var deleteMessageTask = botClient.DeleteMessageAsync(chatId, messageId);
					var sendMessageTask = botClient.SendTextMessageAsync(chatId, msg);
					await Task.WhenAll(new[] { deleteMessageTask, sendMessageTask });
				}
			}

			if (!isBotBlocked)
			{
				var dbTask = db.KeyDeleteAsync(new RedisKey($"PendingQuestion:{userId}"));
				ChatMember recipient = await userInfoTask;
				string firstname = recipient.User.FirstName;
				string lastname = recipient.User.LastName;
				string mention = Helper.GenerateMention(userId, firstname, lastname);
				msg = $"Вопрос успешно отправлен пользователю {mention} из чата " +
					$"<b>{Helper.ConvertTextToHtmlParseMode(toChatTitle)}</b>";
				await dbTask;
				var deleteMessageTask = botClient.DeleteMessageAsync(chatId, messageId);
				var sendMessageTask = botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);
				await Task.WhenAll(new[] { deleteMessageTask, sendMessageTask });
			}
		}

		private async Task<Chat[]> GetChatsAvailableInfo(Message message,
			IDatabaseAsync db, ITelegramBotClient botClient)
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

		private async void HandleNoChatsAvailable(Message message, IDatabaseAsync db, ITelegramBotClient botClient, string msg)
		{
			long chatId = message.Chat.Id;
			int userId = message.From.Id;
			int messageId = message.MessageId;
			var dbTask = db.KeyDeleteAsync(new RedisKey($"PendingQuestion:{userId}"));
			msg += "Нет подходящих чатов, где можно задать анонимный вопрос!\n" +
				"Вы можете добавить бота в группу";
			var addBtn = InlineKeyboardButton.WithUrl("Добавить в группу", $"{Startup.BOT_URL}?startgroup=1");
			var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { addBtn } });
			await dbTask;
			var deleteMessageTask = botClient.DeleteMessageAsync(chatId, messageId);
			var sendMessageTask = botClient.SendTextMessageAsync(chatId, msg, replyMarkup: keyboard);
			await Task.WhenAll(new[] { deleteMessageTask, sendMessageTask });
		}

		private async void HandleNoUsersAvailable(Message message, ITelegramBotClient botClient, Chat[] chats, string msg)
		{
			long chatId = message.Chat.Id;
			int messageId = message.MessageId;
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
			var deleteMessageTask = botClient.DeleteMessageAsync(chatId, messageId);
			var sendMessageTask = botClient.SendTextMessageAsync(chatId, msg, replyMarkup: new InlineKeyboardMarkup(keyboard));
			await Task.WhenAll(new[] { deleteMessageTask, sendMessageTask });
		}

		private async void ProcessSelectUser(Message message, ITelegramBotClient botClient,
			int[] userList, long toChatId, int toUserId, string msg)
		{
			long chatId = message.Chat.Id;
			int userId = message.From.Id;
			int messageId = message.MessageId;
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
			var deleteMessageTask = botClient.DeleteMessageAsync(chatId, messageId);
			var sendMessageTask = botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html, 
				replyMarkup: new InlineKeyboardMarkup(keyboard));
			await Task.WhenAll(new[] { deleteMessageTask, sendMessageTask });
		}

		private async void HandleBotIsNotChatMember(Message message,
			IDatabaseAsync db, ITelegramBotClient botClient)
		{
			long chatId = message.Chat.Id;
			int messageId = message.MessageId;
			Chat[] chats = await GetChatsAvailableInfo(message, db, botClient);
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
			var deleteMessageTask = botClient.DeleteMessageAsync(chatId, messageId);
			var sendMessageTask = botClient.SendTextMessageAsync(chatId, msg, replyMarkup: new InlineKeyboardMarkup(keyboard));
			await Task.WhenAll(new[] { deleteMessageTask, sendMessageTask });
		}

		private async void HandlePendingAnswer(Message message, IDatabaseAsync db, ITelegramBotClient botClient, 
			HashEntry[] record)
		{
			bool isBotBlocked = false;
			long chatId = message.Chat.Id;
			int userId = message.From.Id;
			string firstname = message.From.FirstName;
			string lastname = message.From.LastName;
			string mention = Helper.GenerateMention(userId, firstname, lastname);
			string msg = $"Ответ пользователя {mention} на ваш вопрос:\n" +
				$"\"{Helper.ConvertTextToHtmlParseMode(message.Text)}\"";		

			if (message.Text == null)
			{
				msg = "Отправьте текстовое сообщение!";
				await botClient.SendTextMessageAsync(chatId, msg);
				return;
			}

			var pendingAnswer = record.ToDictionary();
			int toUserId = int.Parse(pendingAnswer["ToUserId"].ToString());
			int messageId = int.Parse(pendingAnswer["MessageId"].ToString());

			try
			{
				await botClient.SendTextMessageAsync(toUserId, msg, parseMode: ParseMode.Html);
			}
			catch (ApiRequestException apiEx)
			{
				if (apiEx.ErrorCode == 403)
				{
					Logger.Log.Warn($"AskAnonymous HANDLER Forbidden: bot was blocked by the user - #userId={toUserId}");
					isBotBlocked = true;				
					var dbTask0 = db.SetRemoveAsync(new RedisKey("MyChatMembers"), new RedisValue(toUserId.ToString()));
					var dbTask1 = db.KeyDeleteAsync(new RedisKey($"PendingAnswer:{userId}"));
					await Task.WhenAll(new[] { dbTask0, dbTask1 });
					msg = "Сообщение не отправлено, пользователь заблокировал бота!";
					var deleteMessageTask = botClient.DeleteMessageAsync(chatId, messageId);
					var sendMessageTask = botClient.SendTextMessageAsync(chatId, msg);
					await Task.WhenAll(new[] { deleteMessageTask, sendMessageTask });
				}
			}

			if (!isBotBlocked)
			{
				msg = "Ответ успешно отправлен!";
				await db.KeyDeleteAsync(new RedisKey($"PendingAnswer:{userId}"));
				var deleteMessageTask = botClient.DeleteMessageAsync(chatId, messageId);
				var sendMessageTask = botClient.SendTextMessageAsync(chatId, msg);
				await Task.WhenAll(new[] { deleteMessageTask, sendMessageTask });
			}
		}
	}
}