using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Engines;
using MafaniaBot.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Handlers
{
    public class MyChatMemberHandler : IExecutable
    {
        public async Task Execute(Update update, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            long userId = update.MyChatMember.From.Id;
            ChatMemberStatus status = update.MyChatMember.NewChatMember.Status;

            try
            {
                IDatabaseAsync db = redis.GetDatabase();

                if (status == ChatMemberStatus.Member)
                {
                    await db.SetAddAsync(new RedisKey("MyChatMembers"), new RedisValue(userId.ToString()));
                }
                else
                {
                    await db.SetRemoveAsync(new RedisKey("MyChatMembers"), new RedisValue(userId.ToString()));

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
