using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Commands
{
    public class StartCommand : Command
    {
        public override string pattern => @"/start";

        public override bool Contains(Message message)
        {
            return message.Text.StartsWith(pattern) && !message.From.IsBot;
        }

        public override async Task Execute(Message message, ITelegramBotClient botClient)
        {
            long chatId = message.Chat.Id;
            int userId = message.From.Id;
            string firstname = message.From.FirstName;
            string lastname = message.From.LastName;

            string mention = lastname != null ? 
                "[" + firstname + " " + lastname + "](tg://user?id=" + userId + ")" :
                "[" + firstname + "](tg://user?id=" + userId + ")";

            string msg = "Привет, " + mention + "!" + 
                "\n/help - список доступных команд.";

            if (message.Chat.Type == ChatType.Private)
			{
                using (var db = new MafaniaBotDBContext())
				{
                    var record = db.MyChatMembers
                        .OrderBy(r => r.UserId)
                        .Where(r => r.UserId.Equals(userId))
                        .FirstOrDefault();

                    if (record == null)
					{
                        db.Add(new MyChatMember { UserId = userId });

                        await db.SaveChangesAsync();
					}
				}
			}

            await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
        }
    }
}