using System.Collections.Generic;
using MafaniaBot.Commands;
using MafaniaBot.Handlers.CallbackQueryHandlers;
using MafaniaBot.Handlers.MessageHandlers;
using MafaniaBot.Handlers.MyChatMemberHandlers;
using MafaniaBot.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Services
{
    public sealed class UpdateService
    {      
        private static readonly object instanceLock = new object();
        private readonly Dictionary<Command, BotCommandScopeType> commands;
        private readonly Handler<Message>[] _messageHandlers;
        private readonly Handler<CallbackQuery>[] _callbackQueryHandlers;
        private readonly Handler<ChatMemberUpdated>[] _myChatMemberHandlers;
        private static UpdateService instance = null;

        private UpdateService()
        {            
            commands = new Dictionary<Command, BotCommandScopeType>
            {
                { new ClassicWordsCommand(), BotCommandScopeType.Default },
                { new BananaCommand(), BotCommandScopeType.Default },                
                { new TitleCommand(), BotCommandScopeType.Default },
                { new CallCommand(), BotCommandScopeType.Default },
                { new TitlesCommand(), BotCommandScopeType.Default },
                { new TopBananaCommand(), BotCommandScopeType.Default },
                { new ChangeIconCommand(), BotCommandScopeType.Default },
                //{ new HelpCommand(), BotCommandScopeType.Default },
                { new StartCommand(), BotCommandScopeType.Default }
            };
            _messageHandlers = new Handler<Message>[]
            {
                new GroupMessageHandler(),
                new PrivateMessageHandler(),
                new NewChatMemberHandler(),
                new LeftChatMemberHandler()
            };          
            _callbackQueryHandlers = new Handler<CallbackQuery>[]
            {
                new TopBananaCallbackQueryHandler(),
                new ClassicWordsGameStartCallbackQueryHandler()
            };
            _myChatMemberHandlers = new Handler<ChatMemberUpdated>[]
            {
                new MyChatMemberPrivateHandler(),
                new MyChatMemberGroupHandler()
            };
        }

        public static UpdateService Instance
        {
            get
            {
                lock (instanceLock) return instance ?? new UpdateService();
            }
        }

        public Dictionary<Command, BotCommandScopeType> Commands => commands;
        public Handler<Message>[] MessageHandlers => _messageHandlers;
        public Handler<CallbackQuery>[] CallbackQueryHandlers => _callbackQueryHandlers;
        public Handler<ChatMemberUpdated>[] MyChatMemberHandlers => _myChatMemberHandlers;
    }
}
