namespace MafaniaBot.Abstractions
{
    public interface IlocalizeService
    {
        public void Initialize(string classname);
        public string GetResource(string key, string langCode);
    }
}
