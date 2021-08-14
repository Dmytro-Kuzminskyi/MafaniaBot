using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MafaniaBot.Engines;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Handlers.MessageHandlers
{
	/// <summary>
	/// Triggered when user sends message to chat with bot
	/// </summary>
	public sealed class PrivateMessageHandler : Handler<Message>
	{
		private readonly GameEngine gameEngine;

		public PrivateMessageHandler()
        {
			gameEngine = GameEngine.Instance;
		}

		public override bool Contains(Message message)
		{
			return message.Chat.Type == ChatType.Private;
		}

		public override Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			Message message = update.Message;
			long userId = message.From.Id;
			string word;

			var game = gameEngine.FindGameByPlayerId(userId);

			if (game == null)
				return Task.CompletedTask;

			var concreteGame = (WordsGame)game;
			var text = message.Text;

			if (text == null)
				return Task.CompletedTask;

			word = text.Contains(' ') ? text.Split(' ').First() : text;
			word = Regex.Match(word, @"^[а-яА-Я]+$").Value;

			if (word.Length == 0)
				return Task.CompletedTask;

			concreteGame.ProcessWord(userId, word);

			return Task.CompletedTask;
		}
	}
}
