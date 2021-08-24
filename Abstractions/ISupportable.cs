namespace MafaniaBot.Abstractions
{
    public interface ISupportable <T> where T : class
    {
        bool Supported(T update);
    }
}
