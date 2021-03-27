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
				Logger.Log.Debug($"AskAnonymous HANDLER triggered in #chatId={message.Chat.Id} #userId={message.From.Id}");

				IDatabaseAsync db = redis.GetDatabase();
				HashEntry[] record = await db.HashGetAllAsync(new RedisKey($"PendingQuestion:{message.From.Id}"));

				if (record != null)
				{
					HandlePendingQuestion(message, db, botClient, record);
					return;
				}

				var value = await db.StringGetAsync(new RedisKey($"PendingAnswer:{message.From.Id}"));

				if (!value.IsNull)
				{
					HandlePendingAnswer(message, db, botClient, int.Parse(value.ToString()));
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
			var tokenSource = new CancellationTokenSource();
			var token = tokenSource.Token;
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
			var dbCheckChatIdTask = db.SetContainsAsync(new RedisKey("MyGroups"), pendingQuestion["ChatId"]);

			if (!await dbCheckChatIdTask)
			{
				//Bot kicked from the chat
				return;
			}

			var chatInfoTask = botClient.GetChatAsync(toChatId);
			var dbCheckUserIdTask = db.SetContainsAsync(new RedisKey($"AskParticipants:{toChatId}"),
				pendingQuestion["ToUserId"]);

			if (!await dbCheckUserIdTask)
			{
				//User is not subscribed to anon ask
				return;
			}

			//TODO Check if user banned the bot
			var toChatTitle = (await chatInfoTask).Title;
			var userInfoTask = botClient.GetChatMemberAsync(toChatId, toUserId);
			msg = $"Новый анонимный вопрос от пользователя из чата " +
				$"<b>{Helper.ConvertTextToHtmlParseMode(toChatTitle)}</b>\n" +
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
					var dbTask0 = db.SetRemoveAsync(new RedisKey("MyChatMembers"),
						pendingQuestion["ToUserId"]);
					var dbTask1 = db.KeyDeleteAsync(new RedisKey($"PendingQuestion:{userId}"));
					msg = "Сообщение не отправлено, пользователь заблокировал бота!";
					var messageTask = botClient.SendTextMessageAsync(chatId, msg, cancellationToken: token);
					var deleteTask = botClient.DeleteMessageAsync(chatId, messageId, cancellationToken: token);

					if (!dbTask0.IsCompletedSuccessfully || !dbTask1.IsCompletedSuccessfully)
					{
						tokenSource.Cancel();
						await botClient.SendTextMessageAsync(chatId, "❌Ошибка сервера❌", ParseMode.Html);
					}

					await Task.WhenAll(new List<Task> { dbTask0, dbTask1, messageTask, deleteTask });
					tokenSource.Dispose();
				}
			}

			if (!isBotBlocked)
			{
				ChatMember recipient = await userInfoTask;
				string firstname = recipient.User.FirstName;
				string lastname = recipient.User.LastName;
				string mention = Helper.GenerateMention(userId, firstname, lastname);
				msg = $"Вопрос успешно отправлен пользователю {mention} из чата " +
					$"<b>{Helper.ConvertTextToHtmlParseMode(toChatTitle)}</b>";
				var dbTask = db.KeyDeleteAsync(new RedisKey($"PendingQuestion:{userId}"));
				var messageTask = botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html, 
					cancellationToken: token);
				var deleteTask = botClient.DeleteMessageAsync(chatId, messageId, cancellationToken: token);

				if (!dbTask.IsCompletedSuccessfully)
				{
					tokenSource.Cancel();
					await botClient.SendTextMessageAsync(chatId, "❌Ошибка сервера❌", ParseMode.Html);
				}

				await Task.WhenAll(new List<Task> { dbTask, messageTask, deleteTask });
				tokenSource.Dispose();
			}
		}

		private async void HandlePendingAnswer(Message message, IDatabaseAsync db, ITelegramBotClient botClient, int toUserId)
		{
			var tokenSource = new CancellationTokenSource();
			var token = tokenSource.Token;
			bool isBotBlocked = false;
			long chatId = message.Chat.Id;
			int userId = message.From.Id;
			string firstname = message.From.FirstName;
			string lastname = message.From.LastName;
			string mention = Helper.GenerateMention(userId, firstname, lastname);
			var msg = $"Ответ пользователя {mention} на ваш вопрос\n" +
				Helper.ConvertTextToHtmlParseMode(message.Text);

			try
			{
				await botClient.SendTextMessageAsync(toUserId, msg);
			}
			catch (ApiRequestException apiEx)
			{
				if (apiEx.ErrorCode == 403)
				{
					isBotBlocked = true;
					Logger.Log.Warn($"AskAnonymous HANDLER Forbidden: bot was blocked by the user - #userId={toUserId}");
					var dbTask = db.SetRemoveAsync(new RedisKey("MyChatMembers"), new RedisValue(toUserId.ToString()));
					msg = "Сообщение не отправлено, пользователь заблокировал бота!";
					var messageTask = botClient.SendTextMessageAsync(chatId, msg, cancellationToken: token);

					if (!dbTask.IsCompletedSuccessfully)
					{
						tokenSource.Cancel();
						await botClient.SendTextMessageAsync(chatId, "❌Ошибка сервера❌", ParseMode.Html);
					}

					await Task.WhenAll(new List<Task> { dbTask, messageTask });
					tokenSource.Dispose();
				}
			}

			if (!isBotBlocked)
			{
				var dbTask = db.KeyDeleteAsync(new RedisKey($"PendingAnswer:{userId}"));
				var messageTask = botClient.SendTextMessageAsync(chatId, "Ответ успешно отправлен!", cancellationToken: token);

				if (!dbTask.IsCompletedSuccessfully)
				{
					tokenSource.Cancel();
					await botClient.SendTextMessageAsync(chatId, "❌Ошибка сервера❌", ParseMode.Html);
				}

				await Task.WhenAll(new List<Task> { dbTask, messageTask });
				tokenSource.Dispose();
			}
		}
	}
}