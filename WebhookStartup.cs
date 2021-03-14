﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MafaniaBot.Properties;
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
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var token = env == "Development" ? Resources.DEV_BOT_TOKEN : Resources.PROD_BOT_TOKEN;
            var client = new TelegramBotClient(token);
            var webHookUrl = $"{configuration["SelfHost"]}/api/message/update";

            try
            {
               hp.PostAsync($"https://api.telegram.org/bot" + token + "/deleteWebhook",
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