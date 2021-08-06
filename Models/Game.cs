using System;
using MafaniaBot.Abstractions;
using MafaniaBot.Engines;

namespace MafaniaBot.Models
{
    public class Game : IPreparable
    {
        protected readonly GameEngine gameEngine;

        protected Game(long chatId, Player[] players)
        {
            gameEngine = GameEngine.Instance;
            ChatId = chatId;
            Players = players; 
        }

        public Type GameType { get; protected set; }
        public int GameResultsMessageId { get; set; }
        public long ChatId { get; }
        public Player[] Players { get; }

        public virtual void Prepare()
        {}
    }
}
