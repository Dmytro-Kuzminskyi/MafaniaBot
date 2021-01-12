using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
			var me = botClient.GetMeAsync().Result;
			string msg = null;

			if (userlist[0].Id.Equals(me.Id))
			{
				msg += "\n/help - список доступных команд.";

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

					msg += mention + ", добро пожаловать в семью!";
				
					await Task.Delay(3000);
					await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Markdown);
				}
			}
		}
	}
}