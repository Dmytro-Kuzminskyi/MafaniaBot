using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StackExchange.Redis;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
	public class AskCancelCallbackQuery : Entity<CallbackQuery>
	{
		public override bool Contains(CallbackQuery callbackQuery)
		{
			if (callbackQuery.Message.Chat.Type != ChatType.Private)
				return false;

			return callbackQuery.Data.Equals("ask_cancel&");
		}

		public override async Task Execute(CallbackQuery callbackQuery, 
			ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			try
			{
				Logger.Log.Debug($"Initiated ask_cancel& by #userId={callbackQuery.From.Id} with #data={callbackQuery.Data}");
				
				long chatId = callbackQuery.Message.Chat.Id;
				int userId = callbackQuery.From.Id;
				int messageId = callbackQuery.Message.MessageId;
				var tokenSource = new CancellationTokenSource();
				var token = tokenSource.Token;
				string msg = "Вы отменили анонимный вопрос!";
				IDatabaseAsync db = redis.GetDatabase();
				var dbTask = db.KeyDeleteAsync(new RedisKey($"PendingQuestion:{userId}"));
				var deleteTask = botClient.DeleteMessageAsync(chatId, messageId, cancellationToken: token);
				var messageTask = botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg, cancellationToken: token);

				if (!dbTask.IsCompletedSuccessfully)
				{
					tokenSource.Cancel();
					await botClient.SendTextMessageAsync(chatId, "❌Ошибка сервера❌", ParseMode.Html);
				}

				await Task.WhenAll(new List<Task> { dbTask, deleteTask, messageTask });
				tokenSource.Dispose();
			}
			catch (Exception ex)
			{
				Logger.Log.Error("ask_cancel& ---", ex);
			}
		}
	}
}