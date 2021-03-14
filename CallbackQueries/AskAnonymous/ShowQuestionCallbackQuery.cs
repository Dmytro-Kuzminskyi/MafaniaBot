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
			return callbackQuery.Message.Text.StartsWith("Новый анонимный вопрос для");
		}

		public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient)
		{
			int recipientId = int.Parse(callbackQuery.Data.Split(':')[0]);
			string message = callbackQuery.Data.Split(':')[1];

			if (callbackQuery.From.Id.Equals(recipientId))
			{
				await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, message, true);
				await botClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
			}
			else
			{
				message = "Этот вопрос не для тебя!";
				await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, message, true);
			}
		}
	}
}
