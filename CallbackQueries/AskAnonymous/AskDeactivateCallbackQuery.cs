using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
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

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient, IConnectionMultiplexer cache)
		{           
			long chatId = callbackQuery.Message.Chat.Id;
			int userId = callbackQuery.From.Id;

            Logger.Log.Debug($"Initiated &ask_anon_deactivate& from #chatId={chatId} by #userId={userId} with #data={callbackQuery.Data}");

            string firstname = callbackQuery.From.FirstName;
			string lastname = callbackQuery.From.LastName;
			string msg = null;

            string mention = lastname != null ?
                $"<a href=\"tg://user?id={userId}\">" + Helper.ConvertTextToHtmlParseMode(firstname) + " " + Helper.ConvertTextToHtmlParseMode(lastname) + "</a>" :
                $"<a href=\"tg://user?id={userId}\">" + Helper.ConvertTextToHtmlParseMode(firstname) + "</a>";

            try
            {
                using (var db = new MafaniaBotDBContext())
                {
                    var record = db.AskAnonymousParticipants
                            .OrderBy(r => r.ChatId)
                            .Where(r => r.ChatId.Equals(chatId))
                            .Where(r => r.UserId.Equals(userId))
                            .FirstOrDefault();

                    if (record != null)
                    {
                        try
                        {
                            Logger.Log.Debug($"&ask_anon_deactivate& Delete record: (#id={record.Id} #chatId={record.ChatId} #userId={record.UserId}) from db.AskAnonymousParticipants");

                            db.AskAnonymousParticipants.Remove(record);
                            await db.SaveChangesAsync();
                        }
                        catch(Exception ex)
                        {
                            Logger.Log.Error("&ask_anon_deactivate& Error while processing db.AskAnonymousParticipants", ex);
                        }

                        msg += "Пользователь " + mention + " отписался от анонимных вопросов!";
                    }
                    else
                    {
                        Logger.Log.Debug("&ask_anon_deactivate& Record not exists in db.AskAnonymousParticipants");

                        msg += "Пользователь " + mention + " не подписан на анонимные вопросы!";
                    }
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