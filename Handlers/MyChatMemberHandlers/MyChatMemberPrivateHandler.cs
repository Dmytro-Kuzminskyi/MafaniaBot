using System;
using System.Threading.Tasks;
using MafaniaBot.Engines;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Handlers.MyChatMemberHandlers
{
    /// <summary>
    /// Triggered when user initiate private chat with bot or block bot
    /// </summary>
    public sealed class MyChatMemberPrivateHandler : Handler<ChatMemberUpdated>
    {
        public override bool Contains(ChatMemberUpdated chatMemberUpdated)
        {
            return chatMemberUpdated.Chat.Type == ChatType.Private;
        }

        public override async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            ChatMemberUpdated myChatMember = update.MyChatMember;
            ChatMemberStatus status = myChatMember.NewChatMember.Status;
            long chatId = myChatMember.Chat.Id;
            long userId = myChatMember.From.Id;

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                if (status == ChatMemberStatus.Member)
                {
                    await db.SetAddAsync(new RedisKey("MyChatMembers"), new RedisValue(chatId.ToString()));
                }
                else
                {
                    await db.SetRemoveAsync(new RedisKey("MyChatMembers"), new RedisValue(chatId.ToString()));

                    var game = (WordsGame)GameEngine.Instance.FindGameByPlayerId(userId);

                    if (game != null)
                        game.ForceStop(userId);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }
        }
    }
}
