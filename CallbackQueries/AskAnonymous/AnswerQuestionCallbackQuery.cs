using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
	public class AnswerQuestionCallbackQuery : Entity<CallbackQuery>
	{
		public override bool Contains(CallbackQuery callbackQuery)
		{
			return callbackQuery.Message.Text.StartsWith("Новый анонимный вопрос для") && callbackQuery.Data.StartsWith("&");
		}

		public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient)
		{
			long chatId = callbackQuery.Message.Chat.Id;
			string data = callbackQuery.Data.Substring(1);
			int senderId = int.Parse(data.Split(':')[0]);
			int recipientId = int.Parse(data.Split(':')[1]);
			string question = data.Split(':')[2];
			string msg = null;

			if (callbackQuery.From.Id.Equals(recipientId))
			{
				using (var db = new MafaniaBotDBContext())
				{
					var recordPendingQuestion = db.PendingAnonymousQuestions
						.OrderBy(r => r.FromUserId)
						.Where(r => r.FromUserId.Equals(recipientId))
						.FirstOrDefault();

					if (recordPendingQuestion != null)
					{
						msg += "Сначала закончи с предыдущим вопросом!";
						await botClient.SendTextMessageAsync(recipientId, msg);
						return;
					}

					var recordPendingAnswer = db.PendingAnonymousAnswers
						.OrderBy(r => r.FromUserId)
						.Where(r => r.FromUserId.Equals(recipientId))
						.Where(r => r.ChatId.Equals(chatId))
						.Where(r => r.ToUserId.Equals(senderId))
						.FirstOrDefault();

					if (recordPendingAnswer == null)
					{
						ChatMember member = await botClient.GetChatMemberAsync(chatId, recipientId);

						string firstname = member.User.FirstName;
						string lastname = member.User.LastName;

						string username = lastname != null ? firstname + " " + lastname : firstname;

						db.Add(new PendingAnswer
						{
							ChatId = chatId,
							FromUserId = recipientId,
							FromUserName = username,
							ToUserId = senderId,
							MessageId = callbackQuery.Message.MessageId
						});

						await db.SaveChangesAsync();

						msg += "Напиши ответ на анонимный вопрос:" +
							"\n" + question;

						await botClient.SendTextMessageAsync(recipientId, msg);
					}
					else
					{
						msg += "Сначала ответь на вопрос!";
						await botClient.SendTextMessageAsync(recipientId, msg);
						return;
					}
				}
			}
			else
			{
				msg += "Этот вопрос не для тебя!";
				await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, msg, true);
			}
		}
	}
}
