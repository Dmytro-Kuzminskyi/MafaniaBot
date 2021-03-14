using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
	public class AskSelectUserCallbackQuery : Entity<CallbackQuery>
	{
		public override bool Contains(CallbackQuery callbackQuery)
		{
			return callbackQuery.Message.Text.Equals("Выбери кому ты хочешь задать анонимный вопрос:");
		}

		public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient)
		{
			string data = callbackQuery.Data;
			long chatId = long.Parse(data.Split(':')[0]);
			int toUserId = int.Parse(data.Split(':')[1]);

			long currentChatId = callbackQuery.Message.Chat.Id;
			int messageId = callbackQuery.Message.MessageId;

			using (var db = new MafaniaBotDBContext())
			{
				var recordPending = db.PendingAnonymousQuestions
					.OrderBy(r => r.FromUserId)
					.Where(r => r.FromUserId.Equals(callbackQuery.From.Id))
					.Where(r => r.ChatId.Equals(chatId))
					.Where(r => r.ToUserId.Equals(toUserId))
					.FirstOrDefault();

				if (recordPending == null)
				{
					ChatMember member = await botClient.GetChatMemberAsync(chatId, toUserId);

					string firstname = member.User.FirstName;
					string lastname = member.User.LastName;

					string username = lastname != null ? firstname + " " + lastname : firstname;

					string mention = "[" + username + "](tg://user?id=" + toUserId + ")";
					string msg = "Напиши анонимный вопрос для: " + mention;
					
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