using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
	public class AskAnonymousSelectUserCallbackQuery : Entity<CallbackQuery>
	{
		public override bool Contains(Message message)
		{
			return message.Text.Equals("Выбери кому ты хочешь задать анонимный вопрос:");
		}

		public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient)
		{			
			InlineKeyboardButton callback = callbackQuery.Message.ReplyMarkup.InlineKeyboard.FirstOrDefault().ToList()[0];

			string username = callback.Text;
			long chatId = long.Parse(callback.CallbackData.Split(':')[0]);
			int toUserId = int.Parse(callback.CallbackData.Split(':')[1]);

			long currentChatId = callbackQuery.Message.Chat.Id;
			int messageId = callbackQuery.Message.MessageId;

			using (var db = new MafaniaBotDBContext())
			{
				var record = db.PendingAnonymousQuestions
					.OrderBy(r => r.FromUserId)
					.Where(r => r.FromUserId.Equals(callbackQuery.From.Id))
					.Where(r => r.ChatId.Equals(chatId))
					.Where(r => r.ToUserId.Equals(toUserId))
					.FirstOrDefault();

				if (record == null)
				{
					string mention = "[" + username + "](tg://user?id=" + toUserId + ")";
					string msg = "Напиши свой вопрос для: " + mention;
					
					db.Add(new PendingQuestion
					{
						ChatId = chatId,
						FromUserId = callbackQuery.From.Id,
						ToUserId = toUserId,
						ToUserName = username
					});
				
					await db.SaveChangesAsync();
					
					await botClient.EditMessageTextAsync(currentChatId, messageId, msg, ParseMode.Markdown);
				}
				else
				{
					await botClient.DeleteMessageAsync(currentChatId, messageId);
				}
			}
		}
	}
}