using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StackExchange.Redis;

namespace MafaniaBot.Commands
{
	public class SetFarewellCommand : Command
	{
		public override string Pattern { get; }

		public override string Description { get; }

		public SetFarewellCommand()
		{
			Pattern = @"/setf";
			Description = "Установка прощания группы";
		}

		public override bool Contains(Message message)
		{
			return (message.Text.StartsWith(Pattern) || message.Text.StartsWith(Pattern + Startup.BOT_USERNAME)) &&
				!message.From.IsBot;
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer redis)
		{
			try
			{
				long chatId = message.Chat.Id;
				int messageId = message.MessageId;
				string msg = null;

				Logger.Log.Info($"Initialized /SETF #chatId={chatId} #userId={message.From.Id}");

				if (message.Chat.Type == ChatType.Channel || message.Chat.Type == ChatType.Private)
				{
					msg = "Эта команда доступна только в групповом чате!";
					await botClient.SendTextMessageAsync(chatId, msg);
					return;
				}

				ChatMember member = await botClient.GetChatMemberAsync(chatId, message.From.Id);

				if (member.Status != ChatMemberStatus.Creator)
				{
					msg = "Эта команда доступна только создателю чата!";
					await botClient.SendTextMessageAsync(chatId, msg, replyToMessageId: messageId);
					return;
				}

				IDatabaseAsync db = redis.GetDatabase();
				int pos = message.Text.IndexOf(' ');		

				if (pos == -1)
				{
					Logger.Log.Warn("/SETG No greeting");
					msg = "Введите текст сообщения для прощания!";
					await botClient.SendTextMessageAsync(chatId, msg, replyToMessageId: messageId);
					return;
				}

				msg = message.Text.Substring(pos + 1);
				var dbTask = db.StringSetAsync(new RedisKey($"Farewell:{chatId}"), new RedisValue(msg));
				var getMeTask = botClient.GetMeAsync();
				User me = await getMeTask;
				string firstname = me.FirstName;
				string lastname = me.LastName;
				int userId = me.Id;
				string mention = Helper.GenerateMention(userId, firstname, lastname);
				await dbTask;
				string farewellMsg = (await db.StringGetAsync(new RedisKey($"Farewell:{chatId}"))).ToString();
				msg = "👋Новое прощание вашей группы👋\n" +
					$"{mention}, {Helper.ConvertTextToHtmlParseMode(farewellMsg)}";
				await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html, replyToMessageId: messageId);
			}
			catch (Exception ex)
			{
				Logger.Log.Error("/SETF ---", ex);
			}
		}
	}
}