using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using System.Net.Http;
using System.Collections.Generic;
using System;

namespace MafaniaBot
{
    public static class WebhookStartup
    {
        public static readonly HttpClient hp = new HttpClient();
        public static IServiceCollection AddTelegramBotClient(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var client = new TelegramBotClient(configuration["Bot:Token"]);
            var webHookUrl = $"{configuration["Host"]}/api/message/update";

            try
            {
               hp.PostAsync($"https://api.telegram.org/bot" + configuration["Bot:Token"] + "/deleteWebhook",
               new FormUrlEncodedContent(new Dictionary<string, string> { { "drop_pending_updates", "true" } })).Wait();
            } 
            catch (HttpRequestException e) 
            { 
                Console.WriteLine(e.StackTrace);
            }
            finally 
            { 
                client.SetWebhookAsync(webHookUrl).Wait(); 
            }

            return serviceCollection
                .AddTransient<ITelegramBotClient>(x => client);
        }
    }
}