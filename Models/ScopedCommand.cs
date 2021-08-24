using System.Collections.Generic;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Models
{
    public abstract class ScopedCommand : BotCommand, IExecutable, ISupportable<Message>
    {
        protected List<BotCommandScopeType> scopeTypes;

        protected ScopedCommand(IEnumerable<BotCommandScopeType> scopeTypes)
        {
            this.scopeTypes = new List<BotCommandScopeType>(scopeTypes);
        }

        public BotCommandScopeType[] ScopeTypes => scopeTypes.ToArray();
        public abstract bool Supported(Message update);
        public abstract Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis, ITranslateService translateService);
    }
}
