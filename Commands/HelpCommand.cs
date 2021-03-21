using System;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Commands
{
    public class HelpCommand : Command
    {
        public override string Pattern { get; }

        public override string Description { get;  }

        public HelpCommand()
        {
            Pattern = @"/help";
            Description = "Помощь";
        }

        public override bool Contains(Message message)
        {
            return message.Text.StartsWith(Pattern) && !message.From.IsBot;
        }

        public override async Task Execute(Message message, ITelegramBotClient botClient)
        {
            try
            {
                long chatId = message.Chat.Id;

                string msg = 
                    "Анонимные вопросы\n" +
                    "       /askmenu - меню анонимных вопросов.\n" +
                    "       Данная команда доступна только в группах.\n" +
                    "       Для активации анонимных вопросов нужна регистрация (команда /start в личной переписке с ботом).\n" +
                    "       Каждый участник группы может задать вопрос только тем, кто подписался на анонимные вопросы.\n\n" +
                    "Прогноз погоды\n" +
                    "       /weather <Город> - текущая погода.\n\n" +
                    "По всем вопросам и предложениям писать @beatstick";

                Logger.Log.Debug($"/START SendTextMessage #chatId={chatId} #msg={msg}");

                await botClient.SendTextMessageAsync(chatId, msg);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("/HELP ---", ex);
            }
        }
    }
}
