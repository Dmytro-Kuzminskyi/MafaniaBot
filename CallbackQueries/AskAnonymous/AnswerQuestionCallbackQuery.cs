using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using StackExchange.Redis;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
	public class AnswerQuestionCallbackQuery : Entity<CallbackQuery>
	{
		public override bool Contains(CallbackQuery callbackQuery)
		{
			if (callbackQuery.Message.Chat.Type != ChatType.Private)
				return false;

			return callbackQuery.Data.StartsWith("answer&");
		}

		public override async Task Execute(CallbackQuery callbackQuery,
			ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			try
			{
				long chatId = callbackQuery.Message.Chat.Id;
				int userId = callbackQuery.From.Id;
				int messageId = callbackQuery.Message.MessageId;
				int toUserId = int.Parse(callbackQuery.Data.Split('&')[1]);
				string msg;

				Logger.Log.Debug($"Initiated answer& from #chatId={chatId} by #userId={callbackQuery.From.Id} with #data={callbackQuery.Data}");

				IDatabaseAsync db = redis.GetDatabase();
				if (await HandlePendingAnswer(callbackQuery, db, botClient))
					return;
				if (await HandlePendingQuestion(callbackQuery, db, botClient))
					return;

				var tokenSource = new CancellationTokenSource();
				var token = tokenSource.Token;
				var dbTask = db.HashSetAsync(new RedisKey($"PendingAnswer:{userId}"),
						new[] { new HashEntry("Status", "Initiated"),
								new HashEntry("ToUserId", toUserId.ToString()),
								new HashEntry("MessageId", messageId.ToString())
						});
				msg = "Напишите ответ на вопрос";
				var cancelBtn = InlineKeyboardButton.WithCallbackData("Отмена", "answer_cancel&");
				var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { cancelBtn } });
				var messageTask = botClient.EditMessageTextAsync(userId, messageId, msg, replyMarkup: keyboard, 
					cancellationToken: token);

				if (!dbTask.IsCompletedSuccessfully)
				{
					tokenSource.Cancel();
					await botClient.SendTextMessageAsync(chatId, "❌Ошибка сервера❌", ParseMode.Html);
				}

				await Task.WhenAll(new List<Task> { dbTask, messageTask });
				tokenSource.Dispose();
			}
			catch (Exception ex)
			{
				Logger.Log.Error("answer& ---", ex);
			}
		}

		private async Task<bool> HandlePendingAnswer(CallbackQuery callbackQuery, 
			IDatabaseAsync db, ITelegramBotClient botClient)
		{
			long chatId = callbackQuery.Message.Chat.Id;
			int userId = callbackQuery.From.Id;
			bool state = false;
			bool value = await db.HashExistsAsync(new RedisKey($"PendingAnswer:{userId}"),
						new RedisValue("Status"));

			if (value)
			{
				string msg = "Сначала напишите ответ на вопрос!";
				await botClient.SendTextMessageAsync(chatId, msg);
				state = true;
			}

			return state;
		}

		private async Task<bool> HandlePendingQuestion(CallbackQuery callbackQuery, 
			IDatabaseAsync db, ITelegramBotClient botClient)
		{
			long chatId = callbackQuery.Message.Chat.Id;
			int userId = callbackQuery.From.Id;
			bool state = false;
			bool value = await db.HashExistsAsync(new RedisKey($"PendingQuestion:{userId}"),
						new RedisValue("Status"));

			if (value)
			{
				string msg = "Сначала закончите с вопросом!";
				await botClient.SendTextMessageAsync(chatId, msg);
				state = true;
			}

			return state;
		}
	}
}