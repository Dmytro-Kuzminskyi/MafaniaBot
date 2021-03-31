using MafaniaBot.Models;
using System;
using System.Collections.Generic;

namespace MafaniaBot.Engines
{
    public sealed class GameEngine
    {
        private static GameEngine instance = null;
        private static readonly object padlock = new object();
        private readonly object wordsGameLock = new object();
        private List<WordsGame> wordsGames;

        GameEngine()
        {
            wordsGames = new List<WordsGame>();
        }

        public static GameEngine Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new GameEngine();
                    }
                    return instance;
                }
            }
        }

        public void RegisterGame<T>(T game)
		{
            if (typeof(T).Equals(typeof(WordsGame)))
			{
                lock (wordsGameLock)
                {
                    wordsGames.Add(game as WordsGame);
                }
            }
        }

        public void RemoveGame<T>(T game)
        {
            if (typeof(T).Equals(typeof(WordsGame)))
            {
                lock (wordsGameLock)
                {
                    wordsGames.Remove(game as WordsGame);
                }
            }
        }

        public T FindGameByChatId<T>(long chatId)
		{
            lock (wordsGameLock)
            {
                if (typeof(T).Equals(typeof(WordsGame)))
                {
                    foreach (var game in wordsGames)
                    {
                        if (game.ChatId == chatId)
                            return (T)Convert.ChangeType(game, typeof(T));
                    }
                }

                return default(T);
            }      
        }
    }
}
