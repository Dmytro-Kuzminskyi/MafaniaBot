using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StackExchange.Redis;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
	public class AskDeactivateCallbackQuery : Entity<CallbackQuery>
	{
		public override bool Contains(CallbackQuery callbackQuery)
		{
			if (callbackQuery.Message.Chat.Type == ChatType.Channel || callbackQuery.Message.Chat.Type == ChatType.Private)
				return false;

			return callbackQuery.Data.Equals("ask_deactivate&");
		}

		public override async Task Execute(CallbackQuery callbackQuery,
			ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			try
			{
				long chatId = callbackQuery.Message.Chat.Id;
				int userId = callbackQuery.From.Id;
				bool result = false;

				Logger.Log.Debug($"Initiated &ask_anon_deactivate& from #chatId={chatId} by #userId={userId} with #data={callbackQuery.Data}");

				string firstname = callbackQuery.From.FirstName;
				string lastname = callbackQuery.From.LastName;
				string msg = null;
				string mention = Helper.GenerateMention(userId, firstname, lastname);
				IDatabaseAsync db = redis.GetDatabase();
				result = await db.SetContainsAsync(new RedisKey("AskParticipants:" + chatId.ToString()),
					new RedisValue(userId.ToString()));

				if (result)
				{
					msg = $"Пользователь {mention} отписался от анонимных вопросов!";
					await db.SetRemoveAsync(new RedisKey("AskParticipants:" + chatId.ToString()),
						new RedisValue(userId.ToString()));
					await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html);
					return;
				}

				msg = $"Пользователь {mention} не подписан на анонимные вопросы!";
				await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html);
			}
			catch (Exception ex)
			{
				Logger.Log.Error("&ask_anon_deactivate& ---", ex);
			}
		}
	}
}