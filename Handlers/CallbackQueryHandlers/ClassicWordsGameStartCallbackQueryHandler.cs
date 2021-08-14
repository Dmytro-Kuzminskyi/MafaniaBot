using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Dictionaries;
using MafaniaBot.Engines;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.Handlers.CallbackQueryHandlers
{
	/// <summary>
	/// Triggered when user accept ClassicWordsGame invitation
	/// </summary>
	public sealed class ClassicWordsGameStartCallbackQueryHandler : Handler<CallbackQuery>
	{
		private readonly GameEngine gameEngine;

		public ClassicWordsGameStartCallbackQueryHandler()
        {
			gameEngine = GameEngine.Instance;
		}

		public override bool Contains(CallbackQuery callbackQuery)
		{
			return callbackQuery.Data.StartsWith(BaseDictionary.gameInviteCbQueryData[typeof(ClassicWordsGame)]);
		}

		public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			CallbackQuery callbackQuery = update.CallbackQuery;
			long chatId = callbackQuery.Message.Chat.Id;
			long secondPlayerId = callbackQuery.From.Id;
			string secondPlayerFirstName = callbackQuery.From.FirstName;
			string secondPlayerLastName = callbackQuery.From.LastName;
			string msg;

			try
			{
				IDatabaseAsync db = redis.GetDatabase();

				if (!await db.SetContainsAsync(new RedisKey("MyChatMembers"), new RedisValue(secondPlayerId.ToString())))
				{
					msg = $"Нажми START в личной переписке со мной чтобы играть в игры.";
					await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg, showAlert: true);
					return;
				}
			}
			catch (Exception ex)
			{
				Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
			}

			long firstPlayerId = long.Parse(callbackQuery.Data.Split('&').Last());

			if (firstPlayerId == secondPlayerId)
            {
				msg = "Ты не можешь принять собственный вызов!";
				await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg, showAlert: true);
				return;
            }

			ChatMember member = await botClient.GetChatMemberAsync(chatId, firstPlayerId);
			string firstPlayerFirstname = member.User.FirstName;
			string firstPlayerLastname = member.User.LastName;

			var players = new Player[] { new Player(firstPlayerId, firstPlayerFirstname, firstPlayerLastname),
											new Player(secondPlayerId, secondPlayerFirstName, secondPlayerLastName) };

			if (firstPlayerFirstname != null)
				gameEngine.RegisterGame(new ClassicWordsGame(chatId, players, TimeSpan.FromMinutes(3).TotalMilliseconds, 10, 10));
		}
	}
}
