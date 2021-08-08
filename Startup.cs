using System.Linq;
using MafaniaBot.Abstractions;
using MafaniaBot.Commands;
using MafaniaBot.Dictionaries;
using MafaniaBot.Engines;
using MafaniaBot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;
using Telegram.Bot;

namespace MafaniaBot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        internal static string REDIS_CONNECTION { get; private set; }
        internal static string BOT_URL { get; private set; }
        internal static string BOT_USERNAME { get; private set; }

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
            REDIS_CONNECTION = _configuration["Connections:Redis"];
            BOT_URL = _configuration["Bot:Url"];
            BOT_USERNAME = _configuration["Bot:Username"];
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .ConfigureBot(_configuration)
                .AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(REDIS_CONNECTION))        
                .AddSingleton<IUpdateService, UpdateService>()
                .AddSingleton<IUpdateEngine, UpdateEngine>()
                .AddHostedService<BackgroundWorkerService>()
                .AddControllers()
                .AddNewtonsoftJson(options =>
                    {
                        options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    });
        }

        public void Configure(IApplicationBuilder app, ITelegramBotClient botClient, IUpdateService updateService)
        {
            var commands = updateService.GetCommands().Where(e => e.Key.GetType() != typeof(StartCommand));
            var scopes = updateService.GetCommands().Values.Distinct();

            foreach (var scope in scopes)
            {
                botClient.SetMyCommandsAsync(commands.Where(e => e.Value == scope).Select(e => e.Key), BaseDictionary.BotCommandScopeMap[scope]).Wait();
            }

            app
                .UseRouting()
                .UseEndpoints(endpoints =>
                    {
                        endpoints.MapDefaultControllerRoute();
                    });
        }
    }
}
