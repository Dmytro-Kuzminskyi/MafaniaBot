using System;

namespace MafaniaBot.Models
{
    public class GameInvite
    {
        public GameInvite(Type gameType, long chatId, long userId, string username, DateTime date, TimeSpan timeToLive)
        {
            GameType = gameType;
            ChatId = chatId;
            UserId = userId;
            Username = username;
            Date = date;
            TimeToLive = timeToLive;
        }

        public Type GameType { get; }
        public long ChatId { get; }
        public long UserId { get; }
        public string Username { get; }
        public int MessageId { get; set; }
        public DateTime Date { get; }
        public TimeSpan TimeToLive { get; }
    }
}
