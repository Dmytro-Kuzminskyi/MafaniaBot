using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Commands;
using MafaniaBot.Handlers.CallbackQueryHandlers;
using MafaniaBot.Handlers.MessageHandlers;
using MafaniaBot.Handlers.MyChatMemberHandlers;
using MafaniaBot.Models;
using MafaniaBot.Services.UpdateResolvers;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Services
{
    public sealed class UpdateService : IUpdateService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly ITranslateService _translateService;
        private readonly ScopedCommand[] commands;
        private readonly Handler<CallbackQuery>[] callbackQueryHandlers;
        private readonly Handler<Message>[] messageHandlers;
        private readonly Handler<ChatMemberUpdated>[] myChatMemberHandlers;

        public UpdateService(IConnectionMultiplexer connectionMultiplexer, ITelegramBotClient telegramBotClient, ITranslateService translateService)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _telegramBotClient = telegramBotClient;
            _translateService = translateService;

            commands = new ScopedCommand[]
            {
                new BananaCommand(new [] { BotCommandScopeType.AllPrivateChats, BotCommandScopeType.AllGroupChats }),
                new TopBananasCommand(new [] { BotCommandScopeType.AllPrivateChats, BotCommandScopeType.AllGroupChats }),
                new TitleCommand(new [] { BotCommandScopeType.AllGroupChats }),
                new TitlesCommand(new [] { BotCommandScopeType.AllGroupChats }),
                new CallCommand(new [] { BotCommandScopeType.AllGroupChats }),
                new ChangeIconCommand(new [] { BotCommandScopeType.AllGroupChats }),
                new SettingsCommand(new [] { BotCommandScopeType.AllPrivateChats, BotCommandScopeType.AllGroupChats }),
                //new HelpCommand(BotCommandScopeType.Default),
                new StartCommand(new [] { BotCommandScopeType.Default }),
                new InformCommand(new [] { BotCommandScopeType.Default })
            };
            callbackQueryHandlers = new Handler<CallbackQuery>[]
            {
                new TopBananasCallbackQueryHandler(),
                new LanguageCallbackQueryHandler(),
                new SelectLanguageCallbackQueryHandler(),
                new LanguageBackCallbackQueryHandler(),
                new SettingsExitCallbackQueryHandler()
            };
            messageHandlers = new Handler<Message>[]
            {
                new GroupMessageHandler(),
                new PrivateMessageHandler(),
                new NewChatMemberHandler(),
                new LeftChatMemberHandler()
            };
            myChatMemberHandlers = new Handler<ChatMemberUpdated>[]
            {
                new MyChatMemberPrivateHandler(),
                new MyChatMemberGroupHandler()
            };
        }

        public ScopedCommand[] Commands => commands.Where(e => e.GetType() != typeof(StartCommand) || e.GetType() != typeof(InformCommand)).ToArray();

        public async Task ProcessUpdate(Update update)
        {
            IUpdateResolver resolver = update.Type switch
            {
                UpdateType.Message => new MessageResolver(commands, messageHandlers),
                UpdateType.CallbackQuery => new CallbackQueryResolver(callbackQueryHandlers),
                UpdateType.MyChatMember => new MyChatMemberResolver(myChatMemberHandlers),
                _ => null
            };

            if (resolver == null && !resolver.Supported(update))
                return;

            await resolver.Execute(update, _telegramBotClient, _connectionMultiplexer, _translateService);
        }
    }
}
