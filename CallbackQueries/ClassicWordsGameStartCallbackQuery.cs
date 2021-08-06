using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Dictionaries;
using MafaniaBot.Engines;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.CallbackQueries
{
	public sealed class ClassicWordsGameStartCallbackQuery : IExecutable, IContainable<CallbackQuery>
	{
		public bool Contains(CallbackQuery callbackQuery)
		{
			return callbackQuery.Data.StartsWith(BaseDictionary.gameInviteCbQueryData[typeof(ClassicWordsGame)]);
		}

		public async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			var gameEngine = GameEngine.Instance;
			CallbackQuery callbackQuery = update.CallbackQuery;
			long chatId = callbackQuery.Message.Chat.Id;
			int messageId = callbackQuery.Message.MessageId;
			long secondPlayerId = callbackQuery.From.Id;
			string secondPlayerFirstName = callbackQuery.From.FirstName;
			string secondPlayerLastName = callbackQuery.From.LastName;
			string msg;

			try
			{
				IDatabaseAsync db = redis.GetDatabase();

				if (!await db.SetContainsAsync(new RedisKey("MyChatMembers"), new RedisValue(secondPlayerId.ToString())))
				{
					msg = $"Нажми START в личных сообщениях чтобы играть в игры.";
					await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg, showAlert: true);
					return;
				}
			}
			catch (Exception ex)
			{
				Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
			}

			long firstPlayerId = long.Parse(callbackQuery.Data.Split('&')[1]);

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

			if (callbackQuery.Data.StartsWith(BaseDictionary.gameInviteCbQueryData[typeof(ClassicWordsGame)]) &&
				firstPlayerFirstname != null)
            {
				gameEngine.RegisterGame(new ClassicWordsGame(chatId, players, TimeSpan.FromMinutes(3).TotalMilliseconds, 10, 10));
			}
		}
	}
}
