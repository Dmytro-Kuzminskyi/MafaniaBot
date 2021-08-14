using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Dictionaries;
using MafaniaBot.Engines.UpdateResolvers;
using MafaniaBot.Helpers;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Engines
{
	public class UpdateEngine : IUpdateEngine
	{
		private static readonly IReadOnlyDictionary<UpdateType, Func<IUpdateResolver>> UpdateResolvers = new ReadOnlyDictionary<UpdateType, Func<IUpdateResolver>>(new Dictionary<UpdateType, Func<IUpdateResolver>> 
		{
			{ UpdateType.MyChatMember, () => new MyChatMemberResolver() },
			{ UpdateType.CallbackQuery, () => new CallbackQueryResolver() },
			{ UpdateType.Message, () => new MessageResolver() }
		});
		private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ITelegramBotClient _telegramBotClient;	

		public UpdateEngine(ITelegramBotClient telegramBotClient, IConnectionMultiplexer connectionMultiplexer)
		{
            _connectionMultiplexer = connectionMultiplexer;
			_telegramBotClient = telegramBotClient;
			GameEngine.Instance.RemovedGameInvite += RemovedGameInviteEventRaised;
			GameEngine.Instance.RegisteredGameInvite += RegisteredGameInviteEventRaised;
		}

		public async Task ProcessUpdate(Update update)
        {
			var updateType = update.Type;

			if (!UpdateResolvers.ContainsKey(updateType))
				return;

			var resolver = UpdateResolvers[updateType]();

			if (!resolver.Supported(update))
				return;

			await resolver.Resolve(update, _telegramBotClient, _connectionMultiplexer);
		}

		private void RemovedGameInviteEventRaised(object sender, GenericEventArgs<GameInvite> e)
        {
			_telegramBotClient.DeleteMessageAsync(e.Value.ChatId, e.Value.MessageId).Wait();
		}

		private void RegisteredGameInviteEventRaised(object sender, GenericEventArgs<GameInvite> e)
        {
            var msg = $"⚔️ {TextFormatter.ConvertTextToHtmlParseMode(e.Value.Username)} бросает вызов сыграть в {BaseDictionary.GameInviteMessage[e.Value.GameType]} ⚔️";

            var acceptBtn = InlineKeyboardButton.WithCallbackData("⚔️ Принять вызов ⚔️", $"{BaseDictionary.gameInviteCbQueryData[e.Value.GameType]}{e.Value.UserId}");
            var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { acceptBtn } });

            var botMessageId = _telegramBotClient.SendTextMessageAsync(e.Value.ChatId, msg, ParseMode.Html, replyMarkup: keyboard).GetAwaiter().GetResult().MessageId;

			e.Value.MessageId = botMessageId;
		}
    }
}
