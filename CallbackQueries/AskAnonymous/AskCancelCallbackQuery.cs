using System;
using System.Threading.Tasks;
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
				string msg = "Вы отменили анонимный вопрос!";
				IDatabaseAsync db = redis.GetDatabase();
				await db.KeyDeleteAsync(new RedisKey($"PendingQuestion:{userId}"));
				await botClient.EditMessageTextAsync(chatId, messageId, msg);
			}
			catch (Exception ex)
			{
				Logger.Log.Error("ask_cancel& ---", ex);
			}
		}
	}
}