using System.Linq;
using System.Collections.Generic;
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
			int userId = message.From.Id;

			using (var db = new MafaniaBotDBContext())
			{
				var record = db.PendingAnonymousQuestions
					.OrderBy(r => r.ChatId)
					.Where(r => r.FromUserId.Equals(userId))
					.FirstOrDefault();

				if (record != null)
				{
					string question = message.Text;
					string msg = "Новый анонимный вопрос для "
						+ "[" + record.ToUserName + "](tg://user?id=" + record.ToUserId + ")";

					db.Remove(record);
					await db.SaveChangesAsync();

					var button = InlineKeyboardButton.WithCallbackData("Посмотреть", record.ToUserId + ":" + question);
					var keyboard = new InlineKeyboardMarkup(button);

					await botClient.SendTextMessageAsync(record.ChatId, msg, ParseMode.Markdown, replyMarkup: keyboard);
				}			
			}
		}
	}
}
