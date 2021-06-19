using MafaniaBot.Abstractions;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace MafaniaBot.Services
{
    public class LocalizeService : IlocalizeService
    {
        private ResourceManager resourceManager = null;
        private readonly string[] supportedLanguages = new string[]
        {
            "en", "ru"
        };

        public void Initialize(string classname)
        {
            resourceManager = new ResourceManager($"MafaniaBot.Resources.{classname}", Assembly.GetExecutingAssembly());
        }

        public string GetResource(string key, string langCode)
        {
            if (supportedLanguages.Contains(langCode))
            {
                return resourceManager.GetString(key, new CultureInfo(langCode));
            }
            else
            {
                return resourceManager.GetString(key, new CultureInfo(supportedLanguages[0]));
            }
        }
    }
}
