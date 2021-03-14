using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
	public class AskInitiateCallbackQuery : Entity<CallbackQuery>
	{
		public override bool Contains(CallbackQuery callbackQuery)
		{
			return callbackQuery.Data.Equals("&ask_anon_question&");
		}

		public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient)
		{
			long chatId = callbackQuery.Message.Chat.Id;
			int userId = callbackQuery.From.Id;
			string firstname = callbackQuery.From.FirstName;
			string lastname = callbackQuery.From.LastName;
			string msg = null;

			string mention = lastname != null ?
				"[" + firstname + " " + lastname + "](tg://user?id=" + userId + ")" :
				"[" + firstname + "](tg://user?id=" + userId + ")";

			using (var db = new MafaniaBotDBContext())
			{
				var recordReg = db.MyChatMembers
					.OrderBy(r => r.UserId)
					.Where(r => r.UserId.Equals(userId))
					.FirstOrDefault();

				if (recordReg == null)
				{
					msg += mention + ", сначала зарегистрируйся!";
					await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
					return;
				}
				else
				{
					//TODO Check If bot is banned
				}

				var recordPending = db.PendingAnonymousQuestions
						.OrderBy(r => r.FromUserId)
						.Where(r => r.FromUserId.Equals(userId))
						.FirstOrDefault();

				if (recordPending != null)
				{
					msg += "Закончи с предыдущим вопросом, чтобы задать новый!";
					await botClient.SendTextMessageAsync(chatId, msg);
					return;
				}

				var recordset = db.AskAnonymousParticipants
						.OrderBy(r => r.ChatId)
						.Where(r => !r.UserId.Equals(userId))
						.Select(r => r.UserId);

				List<int> userlist = recordset.ToList();

				if (userlist.Count == 0)
				{
					msg += "Некому задать анонимный вопрос, подожди пока кто-то подпишется!";
					await botClient.SendTextMessageAsync(userId, msg);
				}
				else
				{
					List<KeyValuePair<string, string>> keyboardData = new List<KeyValuePair<string, string>>();
					var tasks = userlist.Select(userId => botClient.GetChatMemberAsync(chatId, userId));
					ChatMember[] result = await Task.WhenAll(tasks);
					if (result.Length > 0)
					{
						result.ToList().ForEach(member =>
						{
							string firstname = member.User.FirstName;
							string lastname = member.User.LastName;
							string mention = lastname != null ? firstname + " " + lastname : firstname;
							keyboardData.Add(new KeyValuePair<string, string>(mention, chatId.ToString() + ":" + member.User.Id.ToString()));
						});

						InlineKeyboardButton[] ik = keyboardData.Select(item => InlineKeyboardButton.WithCallbackData(item.Key, item.Value)).ToArray();
						var keyboard = new InlineKeyboardMarkup(ik);
						msg += "Выбери кому ты хочешь задать анонимный вопрос:";
						await botClient.SendTextMessageAsync(userId, msg, replyMarkup: keyboard);
					}
					else
					{
						msg += "Некому задать анонимный вопрос, подожди пока кто-то подпишется!";
						await botClient.SendTextMessageAsync(userId, msg);
					}
				}
			}
		}
	}
}