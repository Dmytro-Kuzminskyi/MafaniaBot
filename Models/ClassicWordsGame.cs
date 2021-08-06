namespace MafaniaBot.Models
{
    public class ClassicWordsGame : WordsGame
    {
        public ClassicWordsGame(long chatId, Player[] players, double gameInterval, int boardWidth, int boardHeight) :
            base(chatId, players, gameInterval, boardWidth, boardHeight)
        { }
    }
}
