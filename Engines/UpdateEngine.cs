using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Dictionaries;
using MafaniaBot.Helpers;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Engines
{
	public class UpdateEngine : IUpdateEngine
	{
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ITelegramBotClient _telegramBotClient;
		private readonly IUpdateService _updateService;

		public UpdateEngine(IConnectionMultiplexer connectionMultiplexer, IUpdateService updateService, ITelegramBotClient telegramBotClient)
		{
            _connectionMultiplexer = connectionMultiplexer;
			_telegramBotClient = telegramBotClient;
			_updateService = updateService;
			GameEngine.Instance.RemovedGameInvite += RemovedGameInviteEventRaised;
			GameEngine.Instance.RegisteredGameInvite += RegisteredGameInviteEventRaised;
		}

		public bool Supported(Update update)
        {
			return !(update.Message?.From?.IsBot ?? false) && 
				update.Message?.Chat.Type != ChatType.Channel &&
				update.Message?.Chat.Type != ChatType.Sender &&
				!(update.MyChatMember?.From.IsBot ?? false);
		}

		public async Task HandleMessage(Update update)
		{
			foreach (var command in _updateService.GetCommands().Keys)
			{
				if (command.Contains(update.Message))
				{
					await command.Execute(update, _telegramBotClient, _connectionMultiplexer);
					break;
				}
			}
		}
		
		public async Task HandleEvent(Update update)
		{
			foreach (IContainable<Message> _handler in _updateService.GetMessageHandlers())
			{
				if (_handler.Contains(update.Message))
				{
					await ((IExecutable)_handler).Execute(update, _telegramBotClient, _connectionMultiplexer);
					break;
				}
			}
		}
		
		public async Task HandleCallbackQuery(Update update)
		{
			foreach (IContainable<CallbackQuery> _callbackQuery in _updateService.GetCallbackQueries())
			{
				if (_callbackQuery.Contains(update.CallbackQuery))
				{
					await ((IExecutable)_callbackQuery).Execute(update, _telegramBotClient, _connectionMultiplexer);
					break;
				}
			}
		}

		public async Task HandleMyChatMember(Update update)
        {
			await _updateService.GetMyChatMemberHandler().Execute(update, _telegramBotClient, _connectionMultiplexer);
        }

		private void RemovedGameInviteEventRaised(object sender, GenericEventArgs<GameInvite> e)
        {
			_telegramBotClient.DeleteMessageAsync(e.Value.ChatId, e.Value.MessageId).Wait();
		}

		private void RegisteredGameInviteEventRaised(object sender, GenericEventArgs<GameInvite> e)
        {
            var msg = $"⚔️ {TextHelper.ConvertTextToHtmlParseMode(e.Value.Username)} бросает вызов сыграть в {BaseDictionary.GameInviteMessage[e.Value.GameType]} ⚔️";

            var acceptBtn = InlineKeyboardButton.WithCallbackData("⚔️ Принять вызов ⚔️", $"{BaseDictionary.gameInviteCbQueryData[e.Value.GameType]}{e.Value.UserId}");
            var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { acceptBtn } });

            var botMessageId = _telegramBotClient.SendTextMessageAsync(e.Value.ChatId, msg, ParseMode.Html, replyMarkup: keyboard).GetAwaiter().GetResult().MessageId;

			e.Value.MessageId = botMessageId;
		}
	}
}
