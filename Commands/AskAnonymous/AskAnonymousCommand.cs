using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MafaniaBot.Models;
using System.Linq;
using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Commands.AskAnonymous
{
	public class AskAnonymousCommand : Command
	{
		public override string pattern => @"/ask";

		public override bool Contains(Message message)
		{
			if (message.Chat.Type == ChatType.Channel || message.Chat.Type == ChatType.Private)
				return false;

			return message.Text.StartsWith(pattern) && !message.From.IsBot;
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient)
		{
			var chatId = message.Chat.Id;
			var userId = message.From.Id;
			string msg = null;

			using (var db = new MafaniaBotContext())
			{
				var record = db.AskAnonymousParticipants
					.OrderBy(r => r.ChatId)
					.Where(r => r.UserId.Equals(userId))
					.FirstOrDefault();

				if (record == null)
				{
					msg += "Ты не подписан на анонимные вопросы. Введи /askreg чтобы подписаться!";
					await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Markdown);
				}
				else
				{
					var recordset = db.AskAnonymousParticipants
					.OrderBy(r => r.ChatId)
					.Where(r => !r.UserId.Equals(userId))
					.Select(r => r.UserId);

					var userlist = recordset.ToList();

					if (userlist.Count == 0)
					{
						msg += "Некому задать анонимный вопрос, подожди пока кто-то подпишется!";
						await botClient.SendTextMessageAsync(userId, msg, parseMode: ParseMode.Markdown);
					}
					else
					{
						List<KeyValuePair<string, string>> keyboardData = new List<KeyValuePair<string, string>>();
						var tasks = userlist.Select(u => botClient.GetChatMemberAsync(chatId, (int)u));
						var result = await Task.WhenAll(tasks);
						if (result.Length > 0)
						{
							result.ToList().ForEach(u =>
							{
								var firstname = u.User.FirstName;
								var lastname = u.User.LastName;
								var mention = lastname != null ? firstname + " " + lastname : firstname;
								keyboardData.Add(new KeyValuePair<string, string>(mention, u.User.Id.ToString()));
							});

							InlineKeyboardButton[] ik = keyboardData.Select(item => InlineKeyboardButton.WithCallbackData(item.Key, item.Value)).ToArray();
							var keyboard = new InlineKeyboardMarkup(ik);
							msg += "Выбери кому ты хочешь задать анонимный вопрос:";
							await botClient.SendTextMessageAsync(userId, msg, ParseMode.Markdown, replyMarkup: keyboard);
						}
						else
						{
							msg += "Некому задать анонимный вопрос, подожди пока кто-то подпишется!";
							await botClient.SendTextMessageAsync(userId, msg, ParseMode.Markdown);
						}
					}
				}
			}
		}
	}
}