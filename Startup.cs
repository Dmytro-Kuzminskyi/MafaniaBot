using MafaniaBot.Engines;
using MafaniaBot.Services;
using MafaniaBot.Abstractions;
using Newtonsoft.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace MafaniaBot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        internal static string REDIS_CONNECTION { get; private set; }
        internal static string DATABASE_URL { get; private set; }
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
            var updateService = new UpdateService();

            services
                .AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(REDIS_CONNECTION))
                .AddScoped<IUpdateEngine, UpdateEngine>()
                .AddSingleton<IUpdateService>(updateService)
                .ConfigureBotWebhook(_configuration)
                .ConfigureBotCommands(_configuration, updateService)
                .AddControllers()
                .AddNewtonsoftJson(options =>
                    {
                        options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    })
                .AddFluentValidation();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}