using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using StackExchange.Redis;

namespace MafaniaBot.Commands
{
	public class StartCommand : Command
	{
		public override string Pattern { get; }

		public override string Description { get; }

		private string PatternAskAnonRegister { get; }

		public StartCommand()
		{
			Pattern = @"/start";
			PatternAskAnonRegister = @"/start ask_anon_register";
			Description = "";
		}

		public override bool Contains(Message message)
		{
			return (message.Text.Equals(Pattern) ||
				message.Text.Equals(PatternAskAnonRegister) ||
				message.Text.Equals(Pattern + Startup.BOT_USERNAME)) && !message.From.IsBot;
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			try
			{
				long chatId = message.Chat.Id;
				int userId = message.From.Id;
				int messageId = message.MessageId;
				string firstname = message.From.FirstName;
				string lastname = message.From.LastName;
				string mention = Helper.GenerateMention(userId, firstname, lastname);
				string msg;

				Logger.Log.Info($"Initialized /START #chatId={chatId} #userId={userId}");

				if (message.Chat.Type != ChatType.Private)
				{
					msg = $"Эта команда доступна только в <a href=\"{Startup.BOT_URL}\">личных сообщениях</a>!";
					await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html, disableWebPagePreview: true, replyToMessageId: messageId);
					return;
				}
				
				IDatabaseAsync db = redis.GetDatabase();
				await db.SetAddAsync(new RedisKey("MyChatMembers"), new RedisValue(userId.ToString()));
		
				if (message.Text.Equals(PatternAskAnonRegister))
				{
					msg = "Теперь вы можете подписаться на анонимные вопросы!";
					await botClient.SendTextMessageAsync(chatId, msg);
					return;
				}

				msg = "<b>Привет, " + mention + "!</b>\n\n" +
					"<b>Общие команды</b>\n" +
					"/weather [city] — узнать текущую погоду\n" +
					"/help — справка по командам\n\n" +
					"<b>Команды личного чата</b>\n" +
					"/ask — задать анонимный вопрос\n\n" +
					"<b>Команды группового чата</b>\n" +
					"/askmenu — меню анонимных вопросов\n\n";
				var buttonAdd = InlineKeyboardButton.WithUrl("Добавить в группу", Startup.BOT_URL + "?startgroup=1");
				var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { buttonAdd } });
				await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html, replyMarkup: keyboard);
			}
			catch (Exception ex)
			{
				Logger.Log.Error("/START ---", ex);
			}
		}
	}
}