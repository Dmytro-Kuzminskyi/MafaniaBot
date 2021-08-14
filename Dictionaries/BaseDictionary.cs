using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MafaniaBot.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Dictionaries
{
    public class BaseDictionary
    {
        public static readonly IReadOnlyDictionary<Type, string> gameInviteCbQueryData = new ReadOnlyDictionary<Type, string>(new Dictionary<Type, string>
        {
            { typeof(ClassicWordsGame), "classic_words_game_start&"}
        });

        public static readonly IReadOnlyDictionary<Type, string> GameInviteMessage = new ReadOnlyDictionary<Type, string>(new Dictionary<Type, string>
        {
            { typeof(ClassicWordsGame), "классические слова" }
        });

        public static readonly IReadOnlyDictionary<BotCommandScopeType, BotCommandScope> BotCommandScopeMap = new ReadOnlyDictionary<BotCommandScopeType, BotCommandScope>(new Dictionary<BotCommandScopeType, BotCommandScope>
        {
            { BotCommandScopeType.Default, BotCommandScope.Default() },
            { BotCommandScopeType.AllPrivateChats, BotCommandScope.AllPrivateChats() },
            { BotCommandScopeType.AllGroupChats, BotCommandScope.AllGroupChats() },
            { BotCommandScopeType.AllChatAdministrators, BotCommandScope.AllChatAdministrators() },
        });

        public static readonly List<string> Icons = new List<string> 
        {
            "🐶", "🐱", "🐭", "🐹", "🐰", "🦊", "🐻", "🐼", "🐨", "🐯", "🦁", "🐮", "🐷", "🐸", "🐵", "🐓", "🐧", "🦆", "🦅",
            "🦉", "🦇", "🐺", "🐗", "🐴", "🐝", "🐛", "🦋", "🐌", "🐞", "🐜", "🦟", "🦗", "🕷", "🦂", "🐢", "🐍", "🦎", "🐙",
            "🦑", "🦐", "🦞", "🦀", "🐡", "🐠", "🐟", "🐬", "🐳", "🐋", "🦈", "🐊", "🐅", "🐆", "🦓", "🦍", "🦧", "🐘", "🦛",
            "🦏", "🐫", "🦒", "🦘", "🐃", "🐂", "🐄", "🐏", "🦜", "🦩", "🦨", "🐿", "🦔"
        };
    }
}
