using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Handlers
{
	public class AskAnonymousHandler : Entity<Message>
	{
		public override bool Contains(Message message)
		{
			if (message.Chat.Type != ChatType.Private)
				return false;

			return !message.From.IsBot;
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient)
		{
			long chatId = message.Chat.Id;
			int userId = message.From.Id;
			string msg = null;

			using (var db = new MafaniaBotDBContext())
			{
				var recordPendingQuestion = db.PendingAnonymousQuestions
					.OrderBy(r => r.FromUserId)
					.Where(r => r.FromUserId.Equals(userId))
					.FirstOrDefault();

				if (recordPendingQuestion != null)
				{
					string question = message.Text;
					msg += "Новый анонимный вопрос для [" + recordPendingQuestion.ToUserName + 
						"](tg://user?id=" + recordPendingQuestion.ToUserId + ")";		

					var buttonShow = InlineKeyboardButton.WithCallbackData("Посмотреть", 
						"show&" + recordPendingQuestion.ToUserId + ":" + question);
					var buttonAnswer = InlineKeyboardButton.WithCallbackData("Ответить", 
						"&" + recordPendingQuestion.FromUserId + ":" + recordPendingQuestion.ToUserId + ":" + question);

					var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[] { buttonShow, buttonAnswer });

					await botClient.SendTextMessageAsync(chatId, "Вопрос успешно отправлен!");

					await botClient.SendTextMessageAsync(recordPendingQuestion.ChatId, msg, ParseMode.Markdown, replyMarkup: keyboard);

					db.Remove(recordPendingQuestion);
					await db.SaveChangesAsync();
				}

				var recordPendingAnswer = db.PendingAnonymousAnswers
					.OrderBy(r => r.FromUserId)
					.Where(r => r.FromUserId.Equals(userId))
					.FirstOrDefault();

				if (recordPendingAnswer != null)
				{
					await botClient.SendTextMessageAsync(chatId, "Ответ успешно отправлен!");

					string mention = "[" + recordPendingAnswer.FromUserName + "](tg://user?id=" + recordPendingAnswer.FromUserId + ")";

					msg += "Ответ пользователя " + mention + " на ваш вопрос:" +
						"\n" + message.Text;

					await botClient.SendTextMessageAsync(recordPendingAnswer.ToUserId, msg, ParseMode.Markdown);

					await botClient.DeleteMessageAsync(recordPendingAnswer.ChatId, recordPendingAnswer.MessageId);

					db.Remove(recordPendingAnswer);
					await db.SaveChangesAsync();
				}
			}
		}
	}
}
