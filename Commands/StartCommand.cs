using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Commands
{
    public class StartCommand : Command
    {
        public override string Pattern { get; }

        public override string Description { get; }

        private string PatternAskAnonRegister { get; }

        public StartCommand()
        {
            Pattern = @"/start";
            PatternAskAnonRegister = @"/start ask_anon_register";
            Description = "";
        }

        public override bool Contains(Message message)
        {
            return (message.Text.Equals(Pattern) || message.Text.Equals(PatternAskAnonRegister) || message.Text.Equals(Pattern + Startup.BOT_USERNAME)) && !message.From.IsBot;
        }

        public override async Task Execute(Message message, ITelegramBotClient botClient)
        {
            try
            {
                long chatId = message.Chat.Id;
                int userId = message.From.Id;
                int messageId = message.MessageId;
                string firstname = message.From.FirstName;
                string lastname = message.From.LastName;
                string msg = null;

                string mention = lastname != null ?
                    Helper.ConvertTextToHtmlParseMode(firstname) + " " + Helper.ConvertTextToHtmlParseMode(lastname) :
                    Helper.ConvertTextToHtmlParseMode(firstname);

                if (message.Chat.Type != ChatType.Private)
                {
                    msg = $"Эта команда доступна только в <a href=\"{Startup.BOT_URL}\">личных сообщениях</a>!";

                    Logger.Log.Debug($"/START SendTextMessage #chatId={chatId} #msg={msg}");

                    await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html, disableWebPagePreview: true, replyToMessageId: messageId);

                    return;
                }

                try
                {
                    using (var db = new MafaniaBotDBContext())
                    {
                        var record = db.MyChatMembers
                            .OrderBy(r => r.UserId)
                            .Where(r => r.UserId.Equals(userId))
                            .FirstOrDefault();

                        if (record == null)
                        {
                            Logger.Log.Debug($"/START Add record: (#userId={userId}) to db.MyChatMembers");

                            db.Add(new MyChatMember { UserId = userId });
                            await db.SaveChangesAsync();
                        }
                        else
                        {
                            Logger.Log.Debug($"/START Record exists: (#id={record.Id} #userId={record.UserId}) in db.MyChatMembers");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Error("/START Error while processing db.MyChatMembers", ex);
                }

                if (message.Text.Equals(PatternAskAnonRegister))
                {
                    msg = "Теперь вы можете подписаться на анонимные вопросы!";
                }
                else
                {
                    msg = "<b>Привет, " + mention + "!</b>\n\n" +
                        "<b>Общие команды</b>\n" +
                        "/weather [city] — узнать текущую погоду.\n\n" +
                        "<b>Команды группового чата</b>\n" +
                        "/askmenu — меню анонимных вопросов.\n\n";
                }

                var buttonAdd = InlineKeyboardButton.WithUrl("Добавить в группу", Startup.BOT_URL + "?startgroup=1");

                var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { buttonAdd } });

                Logger.Log.Debug($"/START SendTextMessage #chatId={chatId} #msg={msg}");

                await botClient.SendTextMessageAsync(chatId, msg, ParseMode.Html, replyMarkup: keyboard);

            }
            catch (Exception ex)
            {
                Logger.Log.Error("/START ---", ex);
            }
        }
    }
}