using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.CallbackQueries.AskAnonymous
{
    public class AskActivateCallbackQuery : Entity<CallbackQuery>
    {
        public override bool Contains(CallbackQuery callbackQuery)
        {
			return callbackQuery.Data.Equals("&ask_anon_activate&");
		}

        public override async Task Execute(CallbackQuery callbackQuery, ITelegramBotClient botClient)
		{
            long chatId = callbackQuery.Message.Chat.Id;
			int userId = callbackQuery.From.Id;
			string firstname = callbackQuery.From.FirstName;
			string lastname = callbackQuery.From.LastName;
			string msg = null;

			string mention = lastname != null ?
				"[" + firstname + " " + lastname + "](tg://user?id=" + userId + ")" :
				"[" + firstname + "](tg://user?id=" + userId + ")";

			using (var db = new MafaniaBotDBContext())
			{
				var recordReg = db.MyChatMembers
					.OrderBy(r => r.UserId)
					.Where(r => r.UserId.Equals(userId))
					.FirstOrDefault();

				if (recordReg == null)
				{
					msg += mention + ", сначала зарегистрируйся!";
					await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
					return;
				}

				var record = db.AskAnonymousParticipants
						.OrderBy(r => r.ChatId)
                        .Where(r => r.ChatId.Equals(chatId))
						.Where(r => r.UserId.Equals(userId))
						.FirstOrDefault();

				if (record == null)
				{
					db.Add(new Participant { ChatId = chatId, UserId = userId });
					await db.SaveChangesAsync();
					msg += "Пользователь " + mention + " подписался на анонимные вопросы!";
				}
				else
				{
					msg += "Пользователь " +  mention + " уже подписан на анонимные вопросы!";
				}
			}

			await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
        }
    }
}
