using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Engines;
using MafaniaBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StackExchange.Redis;


namespace MafaniaBot.CallbackQueries
{
	public class WordsGameStartCallbackQuery : Entity<CallbackQuery>
	{
		private GameEngine gameEngine;

		public WordsGameStartCallbackQuery()
		{
			gameEngine = GameEngine.Instance;
		}

		public override bool Contains(CallbackQuery callbackQuery)
		{
			if (callbackQuery.Message.Chat.Type == ChatType.Channel || callbackQuery.Message.Chat.Type == ChatType.Private)
				return false;

			return callbackQuery.Data.StartsWith("words_game_start&");
		}

		public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			try
			{
				long chatId = callbackQuery.Message.Chat.Id;
				int messageId = callbackQuery.Message.MessageId;
				int secondPlayerId = callbackQuery.From.Id;
				string secondPlayerFirstname = callbackQuery.From.FirstName;
				string secondPlayerLastName = callbackQuery.From.LastName;
				string secondPlayerMention = Helper.GenerateMention(secondPlayerId, secondPlayerFirstname, secondPlayerLastName);
				string msg;

				Logger.Log.Debug($"Initiated words_game_start& from #chatId={chatId} by #userId={secondPlayerId} with #data={callbackQuery.Data}");

				int firstPlayerId = int.Parse(callbackQuery.Data.Split('&')[1]);

				if (firstPlayerId == secondPlayerId)
				{
					msg = "Вы не можете принять свой же вызов!";
					await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg, showAlert: true);
					return;
				}

				ChatMember member = await botClient.GetChatMemberAsync(chatId, firstPlayerId);
				string firstPlayerFirstname = member.User.FirstName;
				string firstPlayerLastname = member.User.LastName;
				string firstPlayerMention = Helper.GenerateMention(firstPlayerId, firstPlayerFirstname, firstPlayerLastname);
				gameEngine.RegisterGame(new WordsGame(botClient, chatId, new Tuple<int, string>(firstPlayerId, firstPlayerFirstname), 
					new Tuple<int, string>(secondPlayerId, secondPlayerFirstname)));
				WordsGame game = gameEngine.FindGameByChatId<WordsGame>(chatId);
				msg = "Игра началась! У вас есть 3 мин!\n" +
					$"👈 {firstPlayerMention} ⚔️ {secondPlayerMention} 👉\n\n";
				msg += Helper.GenerateWordsGameBoard(game);
				var deleteTask = botClient.DeleteMessageAsync(chatId, messageId);
				var messageTask = botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);
				await Task.WhenAll(new[] { deleteTask, messageTask });
			}
			catch (Exception ex)
			{
				Logger.Log.Error("words_game_start& ---", ex);
			}
		}
	}
}