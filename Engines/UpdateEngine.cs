using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using MafaniaBot.Abstractions;
using StackExchange.Redis;

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
		}

		public async Task HandleIncomingMessage(Update update)
		{
			Message message = update.Message;

			foreach (var command in _updateService.GetCommands())
			{
				if (command.Contains(message))
				{
					await command.Execute(message, _telegramBotClient, _connectionMultiplexer);
					break;
				}
			}
		}

		public async Task HandleIncomingEvent(Update update)
		{
			Message message = update.Message;

			foreach (var handler in _updateService.GetHandlers())
			{
				if (handler.Contains(message))
				{
					await handler.Execute(message, _telegramBotClient, _connectionMultiplexer);
					break;
				}
			}
		}

		public async Task HandleIncomingCallbackQuery(Update update)
		{
			CallbackQuery callbackQuery = update.CallbackQuery;

			foreach (var cb in _updateService.GetCallbackQueries())
			{
				if (cb.Contains(callbackQuery))
				{
					await cb.Execute(callbackQuery, _telegramBotClient, _connectionMultiplexer);
					break;
				}
			}
		}
	}
}