namespace MafaniaBot.Models
{
    public class Player
    {
        public Player(long userId, string firstname, string lastname)
        {
            UserId = userId;
            FirstName = firstname;
            LastName = lastname;
        }

        public long UserId { get; }
        public string FirstName { get; }
        public string LastName { get; }   
        public int Score { get; private set; }

        public void AddScore(int count)
        {
            Score += count;
        }
    }
}
