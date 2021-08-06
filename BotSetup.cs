using System;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Commands;
using MafaniaBot.Dictionaries;
using MafaniaBot.Engines;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace MafaniaBot
{
    public static class BotSetup
    {
        public static IServiceCollection ConfigureBot(this IServiceCollection serviceCollection, IConfiguration configuration, IUpdateService updateService)
        {
            var botClient = new TelegramBotClient(configuration["Bot:Token"]);
            var webHookUrl = $"{configuration["Host"]}/api/message/update";

            try
            {
                botClient.SetWebhookAsync(webHookUrl, dropPendingUpdates: true).Wait();
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Set webhook error", ex);
            }

            Parallel.ForEach(BaseDictionary.BotCommandScopeMap.Keys, scope => 
            {
                botClient.DeleteMyCommandsAsync(BaseDictionary.BotCommandScopeMap[scope]).Wait();
            });

            var commands = updateService.GetCommands().Where(e => e.Key.GetType() != typeof(StartCommand));           
            var scopes = updateService.GetCommands().Values.Distinct();

            foreach (var scope in scopes)
            {               
                botClient.SetMyCommandsAsync(commands.Where(e => e.Value == scope).Select(e => e.Key), BaseDictionary.BotCommandScopeMap[scope]).Wait();
            }

            GameEngine.Initialize(botClient);

            return serviceCollection
                .AddTransient<ITelegramBotClient>(e => botClient);
        }
    }
}
