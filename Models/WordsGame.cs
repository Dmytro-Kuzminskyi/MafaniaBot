using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using MafaniaBot.Dictionaries;
using MafaniaBot.Extensions;
using Timer = System.Timers.Timer;


namespace MafaniaBot.Models
{
    public class WordsGame : Game
    {
        private readonly double gameInterval;
        private ConcurrentBag<string> foundWords;
        private Timer timer;
        private DateTime timerInitDate;

        public WordsGame(long chatId, Player[] players, double gameInterval, int boardWidth, int boardHeight) : base(chatId, players)
        {
            GameType = GetType();
            this.gameInterval = gameInterval;
            BoardWidth = boardWidth;
            BoardHeight = boardHeight;
            foundWords = new ConcurrentBag<string>();
            List<string> gameField = GenerateGameField(boardWidth, boardHeight);
            PlayersGameFieldDictionary = new ConcurrentDictionary<long, List<string>>();
            Parallel.ForEach(players, player => PlayersGameFieldDictionary.TryAdd(player.UserId, gameField));
            timer = new Timer(gameInterval);
        }

        public ConcurrentDictionary<long, List<string>> PlayersGameFieldDictionary { get; }
        public int BoardWidth { get; }
        public int BoardHeight { get; }

        public event EventHandler<EventArgs> GamePrepared;
        public event EventHandler<EventArgs> GameStarted;
        public event EventHandler<EventArgs> GameEnded;
        public event EventHandler<GenericEventArgs<long>> GameStopped;
        public event EventHandler<GenericEventArgs<Guess>> WordFound;
        public event EventHandler<GenericEventArgs<Guess>> WordExists;
        public event EventHandler<GenericEventArgs<Guess>> WordNotExist;

        public override void Prepare()
        {
            GamePrepared?.Invoke(this, EventArgs.Empty);
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Start();
        }

        public void ForceStop(long userId)
        {
            timer.Dispose();
            gameEngine.RemoveGame(this);
            GameStopped?.Invoke(this, new GenericEventArgs<long>(userId));
        }

        public void ProcessWord(long playerId, string word)
        {
            var wordUpper = word.ToUpper();
            string wordToCheck = "";

            if (foundWords.Contains(wordUpper))
            {
                WordFound?.Invoke(this, new GenericEventArgs<Guess>(new Guess(playerId, word)));
                return;
            }

            Player player = Players.Where(e => e.UserId == playerId).First();
            PlayersGameFieldDictionary.TryGetValue(playerId, out var playerGameField);
            var tempGameField = new List<string>(playerGameField);

            foreach (var ch in wordUpper)
            {
                var position = tempGameField.IndexOf(ch.ToString());

                if (position > -1)
                {
                    wordToCheck += tempGameField[position];
                    tempGameField.RemoveAt(position);
                    tempGameField.Insert(position, "*");
                }
                else
                {
                    WordNotExist?.Invoke(this, new GenericEventArgs<Guess>(new Guess(playerId, word)));
                    return;
                }
            }

            var wordToCheckLower = wordToCheck.ToLower();

            if (!CheckWordInDictionary(wordToCheckLower))
            {
                WordNotExist?.Invoke(this, new GenericEventArgs<Guess>(new Guess(playerId, word)));
                return;
            }

            player.AddScore(CalculateScore(wordToCheck));
            PlayersGameFieldDictionary[playerId] = new List<string>(tempGameField);
            foundWords.Add(wordToCheck);
            WordExists?.Invoke(this, new GenericEventArgs<Guess>(new Guess(playerId, word)));
        }

        public string GenerateWordsGameBoardString(long playerId)
        {
            PlayersGameFieldDictionary.TryGetValue(playerId, out var playerGameField);
            var position = 0;
            var tempGameField = new List<string>(playerGameField);
            var output = $"Твой счет: {Players.Where(e => e.UserId == playerId).First().Score}\n\n<pre>";

            for (int i = 0; i < BoardHeight; i++)
            {
                for (int j = 0; j < BoardWidth; j++)
                    output += tempGameField[position++] + " ";

                output += "\n";
            }

            output += "</pre>";

            return output;
        }

        public string GetRemainingTimeString()
        {
            var secondsDiff = (int)(timerInitDate.AddMilliseconds(gameInterval) - DateTime.Now).TotalSeconds;
            int minutes = secondsDiff / 60;
            int seconds = secondsDiff - 60 * minutes;

            return "Осталось " + (minutes > 0 ? $"{minutes} мин {seconds} сек!" : $"{seconds} сек!");
        }

        private void Start()
        {
            timerInitDate = DateTime.Now;
            timer.Start();
            timer.Elapsed += TimerElapsedEventRaised;
            GameStarted?.Invoke(this, EventArgs.Empty);
        }

        private void TimerElapsedEventRaised(object sender, ElapsedEventArgs e)
        {
            timer.Dispose();
            GameEnded?.Invoke(this, EventArgs.Empty);
            gameEngine.RemoveGame(this);                    
        }

        private List<string> GenerateGameField(int x, int y)
        {
            var gameField = new List<string>();

            for (int i = 0; i < x * y; i++)
                gameField.Add(GameDictionary.LetterWeights.RandomElementByWeight(e => e.Value).Key);

            return gameField;
        }

        private bool CheckWordInDictionary(string word)
        {
            using (var streamReader = new StreamReader($"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar}data{Path.DirectorySeparatorChar}words.txt"))
            {
                string line;

                while ((line = streamReader.ReadLine()) != null)
                {
                    if (line == word.ToLower())
                        return true;
                }
            }

            return false;
        }

        private int CalculateScore(string word)
        {
            int wordLength = word.Length;

            if (wordLength > 15)
                return GameDictionary.ScoreConversion[15];

            return GameDictionary.ScoreConversion[wordLength];
        }
    }
}
