using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using MafaniaBot.Abstractions;

namespace MafaniaBot.Services
{
    public class TranslateService : ITranslateService
    {
        private readonly string[] supportedLanguages = new string[]
        {
            "en", "ru"
        };
        private readonly ResourceManager resourceManager;

        public TranslateService()
        {
            resourceManager = new ResourceManager($"MafaniaBot.Resources.Translations", Assembly.GetExecutingAssembly());
        }

        public string[] SupportedLanguages => supportedLanguages;

        public string TestSupportedLanguageCode(string langCode)
        {
            return supportedLanguages.Where(e => e.Equals(langCode)).FirstOrDefault() ?? supportedLanguages.First();
        }

        public string GetResource(string key, string langCode)
        {
            return supportedLanguages.Contains(langCode) ? resourceManager.GetString(key, new CultureInfo(langCode))
                                                        : resourceManager.GetString(key, new CultureInfo(supportedLanguages.First()));
        }
    }
}
