using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StackExchange.Redis;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
    public class AskDeactivateCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message.Chat.Type == ChatType.Channel || callbackQuery.Message.Chat.Type == ChatType.Private)
                return false;

            return callbackQuery.Data.Equals("&ask_anon_deactivate&");
        }

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            try
            {
                long chatId = callbackQuery.Message.Chat.Id;
                int userId = callbackQuery.From.Id;
                bool result = false;

                Logger.Log.Debug($"Initiated &ask_anon_deactivate& from #chatId={chatId} by #userId={userId} with #data={callbackQuery.Data}");

                string firstname = callbackQuery.From.FirstName;
                string lastname = callbackQuery.From.LastName;
                string msg = null;

                string mention = lastname != null ?
                    $"<a href=\"tg://user?id={userId}\">" + Helper.ConvertTextToHtmlParseMode(firstname) + " " + Helper.ConvertTextToHtmlParseMode(lastname) + "</a>" :
                    $"<a href=\"tg://user?id={userId}\">" + Helper.ConvertTextToHtmlParseMode(firstname) + "</a>";

                try
                {
                    IDatabaseAsync db = redis.GetDatabase();

                    var key = new RedisKey("AskAnonymousParticipants:" + chatId.ToString());
                    var value = new RedisValue(userId.ToString());

                    result = await db.SetContainsAsync(key, value);

                }
                catch (Exception ex)
                {
                    Logger.Log.Error("&ask_anon_activate& Error while processing Redis.AskAnonymousParticipants", ex);
                }

                if (result)
                {
                    try
                    {
                        IDatabaseAsync db = redis.GetDatabase();

                        var key = new RedisKey("AskAnonymousParticipants:" + chatId.ToString());
                        var value = new RedisValue(userId.ToString());

                        result = await db.SetRemoveAsync(key, value);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error("&ask_anon_activate& Error while processing Redis.AskAnonymousParticipants", ex);
                    }

                    msg += "Пользователь " + mention + " отписался от анонимных вопросов!";
                }
                else
                {
                    Logger.Log.Debug("&ask_anon_deactivate& Record not exists in db.AskAnonymousParticipants");

                    msg += "Пользователь " + mention + " не подписан на анонимные вопросы!";
                }

                Logger.Log.Debug($"&ask_anon_deactivate& SendTextMessage #chatId={chatId} #msg={msg}");

                await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html);

            }
            catch (Exception ex)
            {
                Logger.Log.Error("&ask_anon_deactivate& ---", ex);
            }
        }
    }
}