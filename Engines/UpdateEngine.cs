using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using MafaniaBot.Abstractions;
using MafaniaBot.Services;

namespace MafaniaBot.Engines
{
	public class UpdateEngine : IUpdateEngine
	{
		private readonly ITelegramBotClient _telegramBotClient;
		private readonly IUpdateService _updateService;

		public UpdateEngine(IUpdateService updateService, ITelegramBotClient telegramBotClient)
		{
			_telegramBotClient = telegramBotClient;
			_updateService = updateService;
		}

		public async Task HandleIncomingMessage(Update update)
		{
			var message = update.Message;

			foreach (var command in _updateService.GetCommands())
			{
				if (command.Contains(message))
				{
					await command.Execute(message, _telegramBotClient);
					break;
				}
			}
		}

		public async Task HandleIncomingEvent(Update update)
		{
			var message = update.Message;

			foreach (var handler in _updateService.GetEntities())
			{
				if (handler.Contains(message))
				{
					await handler.Execute(message, _telegramBotClient);
					break;
				}
			}
		}
	}
}
