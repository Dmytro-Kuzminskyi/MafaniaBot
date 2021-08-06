using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace MafaniaBot.Abstractions
{
	public interface IUpdateEngine
	{
		bool Supported(Update update);

		Task HandleMessage(Update update);

		Task HandleEvent(Update update);

		Task HandleCallbackQuery(Update update);

		Task HandleMyChatMember(Update update);
	}
}
