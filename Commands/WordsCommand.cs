using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using MafaniaBot.Engines;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using StackExchange.Redis;

namespace MafaniaBot.Commands
{
	public class WordsCommand : Command
	{
		private GameEngine gameEngine;

		public override string Pattern { get; }

		public override string Description { get; }

		public WordsCommand()
		{
			gameEngine = GameEngine.Instance;
			Pattern = @"/words";
			Description = "Игра в слова";
		}

		public override bool Contains(Message message)
		{
			return (message.Text.Equals(Pattern) || message.Text.Equals(Pattern + Startup.BOT_USERNAME)) && !message.From.IsBot;
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			try
			{
				long chatId = message.Chat.Id;
				int userId = message.From.Id;
				string firstname = message.From.FirstName;
				int messageId = message.MessageId;
				string msg;

				Logger.Log.Info($"Initialized /WORDS #chatId={chatId} #userId={userId}");

				if (message.Chat.Type == ChatType.Channel || message.Chat.Type == ChatType.Private)
				{
					msg = "Эта команда доступна только в групповом чате!";
					await botClient.SendTextMessageAsync(chatId, msg, replyToMessageId: messageId);
					return;
				}

				var game = gameEngine.FindGameByChatId<WordsGame>(chatId);

				if (game != null)
				{
					msg = "Игра уже началась!";
					await botClient.SendTextMessageAsync(chatId, msg, replyToMessageId: messageId);
					return;
				}

				msg = $"⚔️ {Helper.ConvertTextToHtmlParseMode(firstname)} бросает вызов сыграть в слова ⚔️";
				var acceptBtn = InlineKeyboardButton.WithCallbackData("⚔️ Принять вызов ⚔️", $"words_game_start&{userId}");
				var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { acceptBtn } });
				await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html, replyMarkup: keyboard);
			}
			catch (Exception ex)
			{
				Logger.Log.Error("/WORDS ---", ex);
			}
		}
	}
}