namespace MafaniaBot.Abstractions
{
    public interface IContainable<T> where T : class
    {
        bool Contains(T update);
    }
}
