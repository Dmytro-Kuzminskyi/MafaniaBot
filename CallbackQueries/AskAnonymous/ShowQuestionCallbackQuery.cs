using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
	public class ShowQuestionCallbackQuery : Entity<CallbackQuery>
	{
		public override bool Contains(CallbackQuery callbackQuery)
		{
			return callbackQuery.Message.Text.StartsWith("Новый анонимный вопрос для") && callbackQuery.Data.StartsWith("show&");
		}

		public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient)
		{
			string data = callbackQuery.Data.Split('&')[1];
			int recipientId = int.Parse(data.Split(':')[0]);
			string message = data.Split(':')[1];

			if (callbackQuery.From.Id.Equals(recipientId))
			{
				await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, message, true);
			}
			else
			{
				message = "Этот вопрос не для тебя!";
				await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, message, true);
			}
		}
	}
}
