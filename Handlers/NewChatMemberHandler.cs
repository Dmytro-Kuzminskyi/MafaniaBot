using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StackExchange.Redis;

namespace MafaniaBot.Handlers
{
	public class NewChatMemberHandler : Entity<Message>
	{
		public override bool Contains(Message message)
		{
			if (message.Chat.Type == ChatType.Private || message.Chat.Type == ChatType.Channel)
				return false;

			return (message.NewChatMembers != null) ? true : false;
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			try
			{
				long chatId = message.Chat.Id;
				User user = message.NewChatMembers[0];
				IDatabaseAsync db = redis.GetDatabase();
				string msg;

				Logger.Log.Debug($"NewChatMember HANDLER triggered: #chatId={chatId} new member #userId={user.Id}");

				if (user.Id.Equals(botClient.BotId))
				{
					msg =
						"<b>Общие команды</b>\n" +
						"/weather [city] — узнать текущую погоду\n" +
						"/help — справка по командам\n\n" +
						"<b>Команды личного чата</b>\n" +
						"/ask — задать анонимный вопрос\n\n" +
						"<b>Команды группового чата</b>\n" +
						"/askmenu — меню анонимных вопросов\n\n";

					string defaultGreetingMsg = "добро пожаловать 😊";
					string defaultFarewellMsg = "покинул(а) чат 😕";					
					var dbTask0 = db.SetAddAsync(new RedisKey("MyGroups"), new RedisValue(chatId.ToString()));
					var dbTask1 = db.StringSetAsync(new RedisKey($"Greeting:{chatId}"), new RedisValue(defaultGreetingMsg));
					var dbTask2 = db.StringSetAsync(new RedisKey($"Farewell:{chatId}"), new RedisValue(defaultFarewellMsg));
					await Task.WhenAll(new[] { dbTask0, dbTask1, dbTask2 });				
					await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);
					return;
				}

				string firstname = user.FirstName;
				string lastname = user.LastName;
				int userId = user.Id;

				if (!user.IsBot)
				{
					string greetingMsg = (await db.StringGetAsync(new RedisKey($"Greeting:{chatId}"))).ToString();
					string mention = Helper.GenerateMention(userId, firstname, lastname);
					msg = mention + $", {greetingMsg}";
					await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);
				}
			}
			catch (Exception ex)
			{
				Logger.Log.Error("NewChatMember HANDLER ---", ex);
			}
		}
	}
}