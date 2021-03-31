using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using MafaniaBot.Engines;
using MafaniaBot.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StackExchange.Redis;

namespace MafaniaBot.Handlers
{
	public class WordsGameHandler : Entity<Message>
	{
		private GameEngine gameEngine;

		public WordsGameHandler()
		{
			gameEngine = GameEngine.Instance;
		}

		public override bool Contains(Message message)
		{
			if (message.Chat.Type == ChatType.Private || message.Chat.Type == ChatType.Channel)
				return false;
			else
				return true;
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			try
			{
				long chatId = message.Chat.Id;
				int userId = message.From.Id;
				string word;
				string msg = null;

				Logger.Log.Debug($"WordsGame HANDLER triggered in #chatId={chatId} #userId={userId}");
				var game = gameEngine.FindGameByChatId<WordsGame>(chatId);

				if (game == null)
					return;

				int player_1_id = game.FirstPlayer.Item1;
				int player_2_id = game.SecondPlayer.Item1;
				string player_1_name = game.FirstPlayer.Item2;				
				string player_2_name = game.SecondPlayer.Item2;

				if (message.Text == null)
					return;

				bool isWhiteSpace = message.Text.Contains(' ');

				if (isWhiteSpace)
				{
					word = message.Text.Split(' ')[0];
				}
				else
				{
					word = message.Text;
				}

				Match match = Regex.Match(word, @"^[а-яА-Я]+$");
				word = match.Value.ToUpper();

				if (word.Length == 0)
					return;

				if (userId == player_1_id || userId == player_2_id)
				{
					WordStatus status = game.ProcessWord(word, userId);

					if (status == WordStatus.Found)
					{
						msg = "Это слово уже отгадали!\n\n";
					}

					if (status == WordStatus.NotExists)
					{
						msg = "Такого слова не существует!\n\n";
					}

					if (status == WordStatus.Exists)
					{
						string uid = userId == player_1_id ? player_1_name : player_2_name;
						msg = $"{uid} отгадал слово {word.ToLower()}!\n\n";
					}

					msg += Helper.GenerateWordsGameBoard(game);
					msg += "Осталось времени: " + game.GetRemainingTime();
					await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);
				}
			} 
			catch (Exception ex)
			{
				Logger.Log.Error("WordsGame HANDLER ---", ex);
			}
		}
	}
}