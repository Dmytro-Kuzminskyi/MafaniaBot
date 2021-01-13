using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Commands.AskAnonymous
{
	public class AskAnonymousCommand : Command
	{
		public override string pattern => @"/ask";

		public override bool Contains(Message message)
		{
			if (message.Chat.Type == ChatType.Channel)
				return false;

			return message.Text.StartsWith(pattern) && !message.From.IsBot;
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient)
		{
			var chatId = message.Chat.Id;
			var userId = message.From.Id;
			var question = message.Text;
			int toUserId;

			await botClient.DeleteMessageAsync(chatId, message.MessageId);

			if (message.Chat.Type == ChatType.Private)

				await botClient.SendTextMessageAsync(chatId, "Команда недоступна в личных сообщениях!", parseMode: ParseMode.Markdown);

			else if (message.ReplyToMessage == null || message.ReplyToMessage.From.IsBot)

				await botClient.SendTextMessageAsync(chatId, "Используй эту команду в ответ на сообщение пользователя!", parseMode: ParseMode.Markdown);

			else if (message.ReplyToMessage.From.Id.Equals(userId))

				await botClient.SendTextMessageAsync(chatId, "Невозможно задать вопрос самому себе! Ну либо у тебя шиза...", parseMode: ParseMode.Markdown);
			else
			{
				toUserId = message.ReplyToMessage.From.Id;
			}
		}
	}
}
