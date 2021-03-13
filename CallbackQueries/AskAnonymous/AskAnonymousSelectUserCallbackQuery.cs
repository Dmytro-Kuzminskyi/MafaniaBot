using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MafaniaBot.Models;
using System.Linq;
using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
	public class AskAnonymousSelectUserCallbackQuery : Entity
	{
		public override bool Contains(Message message)
		{
			return message.Text.Equals("Выбери кому ты хочешь задать анонимный вопрос:");
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient)
		{
			var callbackData = message.ReplyMarkup.InlineKeyboard.ToArray().GetValue(1);
			var c = callbackData.ToString();
			await botClient.GetMeAsync();
		}
	}
}