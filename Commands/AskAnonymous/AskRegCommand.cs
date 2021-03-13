using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MafaniaBot.Models;
using System.Linq;

namespace MafaniaBot.Commands.AskAnonymous
{
	public class AskRegCommand : Command
	{
		public override string pattern => @"/askreg";

		public override bool Contains(Message message)
		{
			if (message.Chat.Type == ChatType.Channel || message.Chat.Type == ChatType.Private)
				return false;

			return message.Text.StartsWith(pattern) && !message.From.IsBot;
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient)
		{
			long chatId = message.Chat.Id;
			int userId = message.From.Id;
			string firstname = message.From.FirstName;
			string lastname = message.From.LastName;
			string msg = null;

			string mention = lastname != null ?
				"[" + firstname + " " + lastname + "](tg://user?id=" + userId + ")" :
				"[" + firstname + "](tg://user?id=" + userId + ")";

			using (var db = new MafaniaBotDBContext())
			{
				var record = db.AskAnonymousParticipants
						.OrderBy(r => r.ChatId)
						.Where(r => r.UserId.Equals(userId))
						.FirstOrDefault();

				if (record == null)
				{
					db.Add(new Participant { ChatId = chatId, UserId = userId });
					await db.SaveChangesAsync();
					msg += mention + " подписался на анонимные вопросы!";
				}
				else
				{
					msg += "Ты уже подписан на анонимные вопросы!";
				}
			}

			await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
		}
	}
}