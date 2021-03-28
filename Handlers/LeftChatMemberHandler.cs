using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StackExchange.Redis;

namespace MafaniaBot.Handlers
{
	public class LeftChatMemberHandler : Entity<Message>
	{
		public override bool Contains(Message message)
		{
			if (message.Chat.Type == ChatType.Private || message.Chat.Type == ChatType.Channel)
				return false;

			return (message.LeftChatMember != null) ? true : false;
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			try
			{
				long chatId = message.Chat.Id;
				User member = message.LeftChatMember;
				string msg;

				Logger.Log.Debug($"LeftChatMember HANDLER triggered: #chatId={chatId} left member #userId={member.Id}");

				IDatabaseAsync db = redis.GetDatabase();

				if (member.Id.Equals(botClient.BotId))
				{
					var dbTask0 = db.SetRemoveAsync(new RedisKey("MyGroups"), new RedisValue(chatId.ToString()));
					var dbTask1 = db.KeyDeleteAsync(new RedisKey($"AskParticipants:{chatId}"));
					await Task.WhenAll(new[] { dbTask0, dbTask1 });
					return;
				}

				if (!member.IsBot)
				{
					int userId = member.Id;
					string firstname = member.FirstName;
					string lastname = member.LastName;
					string mention = Helper.GenerateMention(userId, firstname, lastname);
					msg = mention + ", покинул(а) чат 😕";
					await db.SetRemoveAsync(new RedisKey($"AskParticipants:{chatId}"), new RedisValue(userId.ToString()));
					await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);
				}
			}
			catch (Exception ex)
			{
				Logger.Log.Error("LeftChatMember HANDLER ---", ex);
			}
		}
	}
}