﻿using System;
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
				string msg;

				Logger.Log.Debug($"NewChatMember HANDLER triggered: #chatId={chatId} new member #userId={user.Id}");

				if (user.Id.Equals(botClient.BotId))
				{
					msg =
						"<b>Общие команды</b>\n" +
						"/weather [city] — узнать текущую погоду.\n" +
						"/ask — задать анонимный вопрос.\n\n" +
						"<b>Команды группового чата</b>\n" +
						"/askmenu — меню анонимных вопросов.";

					IDatabaseAsync db = redis.GetDatabase();
					await db.SetAddAsync(new RedisKey("MyGroups"), new RedisValue(chatId.ToString()));				
					await botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html);
					return;
				}

				string firstname = user.FirstName;
				string lastname = user.LastName;
				int userId = user.Id;

				if (!user.IsBot)
				{
					string mention = Helper.GenerateMention(userId, firstname, lastname);
					msg = mention + ", добро пожаловать 😊";
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