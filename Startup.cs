using MafaniaBot.Abstractions;
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
        internal static string BOT_URL { get; private set; }
        internal static string BOT_USERNAME { get; private set; }
        internal static long SUPPORT_USERID { get; private set; }

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
            BOT_URL = _configuration["Bot:Url"];
            BOT_USERNAME = _configuration["Bot:Username"];
            SUPPORT_USERID = long.Parse(_configuration["Support:UserId"]);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                    .AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(_configuration["Connections:Redis"]))
                    .AddSingleton<IUpdateService, UpdateService>()
                    .AddSingleton<ITranslateService, TranslateService>()
                    .AddHostedService<ConfigureBot>()
                    .AddHostedService<BackgroundWorkerService>();
            services
                    .AddHttpClient("tgbotclient")
                    .AddTypedClient<ITelegramBotClient>(e => new TelegramBotClient(_configuration["Bot:Token"], e));                  
            services
                    .AddControllers()
                    .AddNewtonsoftJson(options =>
                        {
                            options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                            options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                        });
        }

        public void Configure(IApplicationBuilder app)
        {
            app
                .UseRouting()
                .UseCors()
                .UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllerRoute(
                            name: "tgbotclient",
                            pattern: $"bot{_configuration["Bot:Token"]}",
                            new { controller = "Webhook", action = "Post" });
						endpoints.MapControllers();
                    });
        }
    }
}
