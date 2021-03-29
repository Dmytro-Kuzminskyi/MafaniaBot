using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StackExchange.Redis;

namespace MafaniaBot.Commands
{
	public class GreetingCommand : Command
	{
		public override string Pattern { get; }

		public override string Description { get; }

		public GreetingCommand()
		{
			Pattern = @"/greeting";
			Description = "Просмотр приветствия группы";
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
				int messageId = message.MessageId;
				string msg;

				Logger.Log.Info($"Initialized /GREETING #chatId={chatId} #userId={message.From.Id}");

				if (message.Chat.Type == ChatType.Channel || message.Chat.Type == ChatType.Private)
				{
					msg = "Эта команда доступна только в групповом чате!";
					await botClient.SendTextMessageAsync(chatId, msg);
					return;
				}

				IDatabaseAsync db = redis.GetDatabase();
				var getMeTask = botClient.GetMeAsync();
				var dbTask = db.StringGetAsync(new RedisKey($"Greeting:{chatId}"));
				User me = await getMeTask;
				string firstname = me.FirstName;
				string lastname = me.LastName;
				int userId = me.Id;
				string mention = Helper.GenerateMention(userId, firstname, lastname);
				string greetingMsg = (await dbTask).ToString();
				msg = "👋Приветствие вашей группы👋\n" +
					$"{mention}, {Helper.ConvertTextToHtmlParseMode(greetingMsg)}\n"+
					"Чтобы изменить приветствие группы используйте команду /setg [greeting]";
				await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html, replyToMessageId: messageId);
			}
			catch (Exception ex)
			{
				Logger.Log.Error("/GREETING ---", ex);
			}
		}
	}
}