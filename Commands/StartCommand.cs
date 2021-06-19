using MafaniaBot.Abstractions;
using MafaniaBot.Helpers;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Commands
{
	public class StartCommand : Command
	{
		public override string Pattern => @"/start";
		public override string Description => "";
		public override bool Contains(Message message)
		{
			return (message.Text.Equals(Pattern) || 
				message.Text.Equals(Pattern + Startup.BOT_USERNAME)) &&
				message.Chat.Type == ChatType.Private &&
				!message.From.IsBot;
		}

        public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer redis, IlocalizeService localizer)
		{
			try
			{			
				var chatId = message.Chat.Id;
				var fromId = message.From.Id;
				var messageId = message.MessageId;
				var firstname = message.From.FirstName;
				string langCode = message.From.LanguageCode;

				Logger.Log.Info($"{GetType().Name}: #chatId={chatId} #fromId={fromId}");

				localizer.Initialize(GetType().Name);
				langCode = await DBHelper.GetSetUserLanguageCodeAsync(redis, fromId, langCode);

				var msg = $"<b>{localizer.GetResource("Greeting", langCode)}, {TextHelper.ConvertTextToHtmlParseMode(firstname)}!</b>\n\n";

				var buttonAdd = InlineKeyboardButton.WithUrl(localizer.GetResource("AddToGroup", langCode), Startup.BOT_URL + "?startgroup=1");
				var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { buttonAdd } });

				await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html, replyMarkup: keyboard);

				Logger.Log.Debug($"{GetType().Name}: #chatId={chatId} #msg={msg}");
			}
			catch (Exception ex)
			{
				Logger.Log.Error($"{GetType().Name}: {ex.GetType().Name}", ex);
			}
		}
    }
}
