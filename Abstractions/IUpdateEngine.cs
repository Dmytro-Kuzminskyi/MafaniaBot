using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace MafaniaBot.Abstractions
{
	public interface IUpdateEngine
	{
		Task ProcessUpdate(Update update);
	}
}
