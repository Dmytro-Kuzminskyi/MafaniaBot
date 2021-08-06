namespace MafaniaBot.Models
{
    public class Guess
    {
        public Guess(long userId, string text)
        {
            UserId = userId;
            Text = text;
        }

        public long UserId { get; }
        public string Text { get; }
    }
}
