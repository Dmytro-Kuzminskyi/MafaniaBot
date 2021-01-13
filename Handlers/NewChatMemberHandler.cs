using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MafaniaBot.Models;
using System.Linq;

namespace MafaniaBot.Handlers
{
	public class NewChatMemberHandler : Entity
	{
		public override bool Contains(Message message)
		{
			if (message.Chat.Type == ChatType.Private || message.Chat.Type == ChatType.Channel)
				return false;

			return (message.NewChatMembers != null) ? true : false;
		}
		
		public override async Task Execute(Message message, ITelegramBotClient botClient)
		{
			var chatId = message.Chat.Id;
			var userlist = message.NewChatMembers;
			string msg = null;

			if (userlist[0].Id.Equals(Startup.MyBotId))
			{
				msg += "\n/help - список доступных команд.";

				using (var db = new MafaniaBotContext())
				{
					var record = db.MyGroups
								 .Where(g => g.ChatId == chatId)
								 .FirstOrDefault();

					if (record == null)
					{
						await db.AddAsync(new MyGroup { ChatId = chatId, Status = "member" });
						await db.SaveChangesAsync();
					}
					else
					{
						record.Status = "member";
						await db.SaveChangesAsync();
					}
				}
				
				await Task.Delay(3000);
				await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Markdown);
			} 
			else
			{
				var user = userlist[0]; 
				var firstname = user.FirstName;
				var lastname = user.LastName;
				var userId = user.Id;
				if (!user.IsBot)
				{
					var mention = lastname != null ?
						"[" + firstname + " " + lastname + "](tg://user?id=" + userId + ")" :
						"[" + firstname + "](tg://user?id=" + userId + ")";

					msg += mention + ", добро пожаловать в Ханство!";
				
					await Task.Delay(3000);
					await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Markdown);
				}
			}
		}
	}
}