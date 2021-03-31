using System;
using System.IO;
using System.Timers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using MafaniaBot.Engines;
using MafaniaBot.Enums;

namespace MafaniaBot.Models
{
	public class WordsGame
	{
		private readonly ITelegramBotClient botClient;
		private readonly GameEngine gameEngine;
		private readonly object _lock = new object();
		private Dictionary<string, float> letters;
		private Dictionary<int, int> scoreConversion;
		private List<string> foundWords;
		private List<string> player_1_field;
		private List<string> player_2_field;
		private int player_1_score;
		private int player_2_score;
		private DateTime timerInit;
		private Timer timer;

		public long ChatId { get; }

		public Tuple<int, string> FirstPlayer { get; private set; }

		public List<string> FirstPlayerField { get { return player_1_field; } private set { player_1_field = value; } }

		public int FirstPlayerScore { get { return player_1_score; } private set { player_1_score = 0; } }

		public Tuple<int, string> SecondPlayer { get; private set; }

		public List<string> SecondPlayerField { get { return player_2_field; } private set { player_2_field = value; } }

		public int SecondPlayerScore { get { return player_2_score; } private set { player_2_score = 0; } }

		public int X { get; private set; }

		public int Y { get; private set; }

		public WordsGame(ITelegramBotClient client, long chatId, Tuple<int, string> firstPlayer, Tuple<int, string> secondPlayer)
		{
			gameEngine = GameEngine.Instance;
			botClient = client;
			ChatId = chatId;
			FirstPlayer = firstPlayer;
			SecondPlayer = secondPlayer;
			X = 6;
			Y = 8;
			InitDictionaries();
			var gameField = GenerateGameField(X, Y);
			foundWords = new List<string>();
			Parallel.Invoke(
				() => FirstPlayerField = new List<string>(gameField),
				() => SecondPlayerField = new List<string>(gameField));
			timer = new Timer(180_000);
			timer.Start();
			timerInit = DateTime.Now;
			timer.Elapsed += Timer_Elapsed;
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			try
			{
				Logger.Log.Info($"Game timer elapsed #chatId={ChatId}");

				timer.Dispose();
				gameEngine.RemoveGame(this);
				string msg = "Игра завершена!\n";

				if (FirstPlayerScore > SecondPlayerScore)
				{
					msg += $"🏆 Победитель {Helper.ConvertTextToHtmlParseMode(FirstPlayer.Item2)}! 🏆\n";
				}

				if (SecondPlayerScore > FirstPlayerScore)
				{
					msg += $"🏆 Победитель {Helper.ConvertTextToHtmlParseMode(SecondPlayer.Item2)}! 🏆\n";
				}

				if (FirstPlayerScore == SecondPlayerScore)
				{
					msg += "🌝 Ничья! 🌚\n";
				}

				msg += Helper.GenerateWordsGameBoard(this);
				botClient.SendTextMessageAsync(ChatId, msg, parseMode: ParseMode.Html);
			}
			catch (Exception ex)
			{
				Logger.Log.Error($"Game timer ERROR #chatId={ChatId}", ex);
			}
		}

		private void InitDictionaries()
		{
			letters = new Dictionary<string, float>();
			#region letters
			letters.Add("А", 0.0952f);
			letters.Add("Б", 0.0161f);
			letters.Add("В", 0.0371f);
			letters.Add("Г", 0.0149f);
			letters.Add("Д", 0.0239f);
			letters.Add("Е", 0.0855f);
			letters.Add("Ж", 0.0066f);
			letters.Add("З", 0.0169f);
			letters.Add("И", 0.0834f);
			letters.Add("Й", 0.0057f);
			letters.Add("К", 0.0496f);
			letters.Add("Л", 0.0431f);
			letters.Add("М", 0.0246f);
			letters.Add("Н", 0.0706f);
			letters.Add("О", 0.0966f);
			letters.Add("П", 0.0316f);
			letters.Add("Р", 0.0618f);
			letters.Add("С", 0.05f);
			letters.Add("Т", 0.061f);
			letters.Add("У", 0.0206f);
			letters.Add("Ф", 0.0068f);
			letters.Add("Х", 0.0064f);
			letters.Add("Ц", 0.0125f);
			letters.Add("Ч", 0.0131f);
			letters.Add("Ш", 0.008f);
			letters.Add("Щ", 0.0055f);
			letters.Add("Ы", 0.0114f);
			letters.Add("Ь", 0.0209f);
			letters.Add("Э", 0.0024f);
			letters.Add("Ю", 0.0027f);
			letters.Add("Я", 0.0141f);
			#endregion
			scoreConversion = new Dictionary<int, int>();
			#region scoreConversion
			scoreConversion.Add(2, 1);
			scoreConversion.Add(3, 2);
			scoreConversion.Add(4, 4);
			scoreConversion.Add(5, 5);
			scoreConversion.Add(6, 7);
			scoreConversion.Add(7, 8);
			scoreConversion.Add(8, 10);
			scoreConversion.Add(9, 11);
			scoreConversion.Add(10, 13);
			scoreConversion.Add(11, 14);
			scoreConversion.Add(12, 16);
			scoreConversion.Add(13, 17);
			scoreConversion.Add(14, 18);
			scoreConversion.Add(15, 20);
			#endregion
		}

		private List<string> GenerateGameField(int x, int y)
		{
			var field = new List<string>();

			for (int i = 0; i < y; i++)
			{
				for (int j = 0; j < x; j++)
				{
					field.Add(letters.RandomElementByWeight(e => e.Value).Key);
				}
			}

			return field;
		}

		public string GetRemainingTime()
		{
			int secondsDiff = (int)(timerInit.AddSeconds(180) - DateTime.Now).TotalSeconds;
			int minutes = secondsDiff / 60;
			int seconds = secondsDiff - 60 * minutes;
			return $"{minutes} мин {seconds} сек!";
		}

		public WordStatus ProcessWord(string word, int userId)
		{
			List<string> temp;
			string wordToCheck = "";

			lock (_lock)
			{
				if (foundWords.Contains(word))
				{
					return WordStatus.Found;
				}

				if (userId == FirstPlayer.Item1)
				{
					temp = new List<string>(player_1_field);

					foreach (var ch in word)
					{
						int pos = temp.IndexOf(ch.ToString());

						if (pos > -1)
						{
							wordToCheck += temp[pos];
							temp.RemoveAt(pos);
							temp.Insert(pos, "*");
						}
						else
						{
							return WordStatus.NotExists;
						}
					}

					bool isExists = CheckWordInDictionary(wordToCheck);

					if (!isExists)
					{
						return WordStatus.NotExists;
					}

					player_1_score += CalculateScore(wordToCheck);
					player_1_field = new List<string>(temp);
				}

				if (userId == SecondPlayer.Item1)
				{
					temp = new List<string>(player_2_field);

					foreach (var ch in word)
					{
						int pos = temp.IndexOf(ch.ToString());

						if (pos > -1)
						{
							wordToCheck += temp[pos];
							temp.RemoveAt(pos);
							temp.Insert(pos, "*");
						}
						else
						{
							return WordStatus.NotExists;
						}
					}

					bool isExists = CheckWordInDictionary(wordToCheck);

					if (!isExists)
					{
						return WordStatus.NotExists;
					}

					player_2_score += CalculateScore(wordToCheck);
					player_2_field = new List<string>(temp);
				}

				foundWords.Add(wordToCheck);
				return WordStatus.Exists;
			}
		}

		private bool CheckWordInDictionary(string word)
		{
			string w = word.ToLower();

			using (var sr = new StreamReader(@"data/words.txt"))
			{
				string ln;
				while ((ln = sr.ReadLine()) != null)
				{
					if (ln.Equals(w))
						return true;
				}
			}

			return false;
		}

		private int CalculateScore(string word)
		{
			int length = word.Length;

			if (length > 15)
				return scoreConversion[15];

			return scoreConversion[length];
		}
	}
}