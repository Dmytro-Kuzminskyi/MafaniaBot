using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace MafaniaBot.Abstractions
{
	public interface IUpdateEngine
	{
		public Task HandleIncomingMessage(Update update);
		public Task HandleIncomingEvent(Update update);
		public Task HandleIncomingCallbackQuery(Update update);
	}
}
