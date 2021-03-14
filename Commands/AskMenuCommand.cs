using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Commands
{
	public class AskMenuCommand : Command
	{
		public override string pattern => @"/askmenu";

		public override bool Contains(Message message)
		{
			if (message.Chat.Type == ChatType.Channel || message.Chat.Type == ChatType.Private)
				return false;

			return message.Text.StartsWith(pattern) && !message.From.IsBot;
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient)
		{
			long chatId = message.Chat.Id;

			var buttonReg = InlineKeyboardButton.WithUrl("Зарегистрироваться", Startup.BOT_URL);
			var buttonActivate = InlineKeyboardButton.WithCallbackData("Подписаться", "&ask_anon_activate&");
			var buttonDeactivate = InlineKeyboardButton.WithCallbackData("Отписаться", "&ask_anon_deactivate&");
			var buttonAskAnon = InlineKeyboardButton.WithCallbackData("Задать анонимный вопрос", "&ask_anon_question&");

			var keyboard = new InlineKeyboardMarkup(new[] {
				new InlineKeyboardButton[] { buttonReg },
				new InlineKeyboardButton[] { buttonActivate, buttonDeactivate },
				new InlineKeyboardButton[] { buttonAskAnon }
			});

			string msg = "Меню анонимных вопросов";
			
			await botClient.SendTextMessageAsync(chatId, msg, replyMarkup: keyboard);
		}
	}
}