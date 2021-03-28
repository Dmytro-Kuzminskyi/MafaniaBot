using System;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;
using Telegram.Bot;
using Newtonsoft.Json;
using MafaniaBot.Models;
using MafaniaBot.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MafaniaBot
{
    public static class BotSetup
    {
        public static IServiceCollection ConfigureBotWebhook(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var client = new TelegramBotClient(configuration["Bot:Token"]);

            var webHookUrl = $"{configuration["Host"]}/api/message/update";

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.PostAsync($"https://api.telegram.org/bot" + configuration["Bot:Token"] + "/deleteWebhook",
                    new FormUrlEncodedContent(new Dictionary<string, string> { { "drop_pending_updates", "true" } })).Wait();
                }
            } 
            catch (HttpRequestException ex) 
            {
                Logger.Log.Error("/deleteWebhook error", ex);
            }

            Thread.Sleep(5_000);

            try
            { 
                client.SetWebhookAsync(webHookUrl).Wait(); 
            }
            catch (Exception ex)
            {
                Logger.Log.Error("/setupWebhook error", ex);
            }

            return serviceCollection
                .AddTransient<ITelegramBotClient>(x => client);
        }

        public static IServiceCollection ConfigureBotCommands(this IServiceCollection serviceCollection, IConfiguration configuration, IUpdateService updateService)
        {
            var commands = new List<Command>(updateService.GetCommands());

            Command startCommand = commands.Find(c => c.GetType().Name.Equals("StartCommand"));
            commands.Remove(startCommand);

            Command[] commandArray = commands.ToArray();

            var botCommands = new List<BotCommand>();

            for (int i = 0; i < commands.Count; i++)
            {
                string pattern = commandArray[i].Pattern.Replace("/", "");
                string description = commandArray[i].Description;

                var botCommand = new BotCommand(pattern, description);
                botCommands.Add(botCommand);
            }

            try
            {
                using (var httpClient = new HttpClient())
                {
                    string jsonString = "{\"commands\":" + JsonConvert.SerializeObject(botCommands) + "}";

                    HttpContent content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    httpClient.PostAsync($"https://api.telegram.org/bot" + configuration["Bot:Token"] + "/setMyCommands", content).Wait();
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("/setMyCommand error", ex);
            }

            return serviceCollection;
        }
    }
}