using System;

namespace MafaniaBot.Abstractions
{
    public interface ITranslateService
    {
        public string[] SupportedLanguages { get; }
        string TestSupportedLanguageCode(string langCode);
        string GetResource(string key, string langCode);
    }
}
