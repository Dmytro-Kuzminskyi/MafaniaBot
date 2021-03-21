using MafaniaBot.Models;
using MafaniaBot.Engines;
using MafaniaBot.Services;
using MafaniaBot.Abstractions;
using Newtonsoft.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MafaniaBot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        internal static string DATABASE_URL { get; private set; }
        internal static string BOT_URL { get; private set; }
        internal static string BOT_USERNAME { get; private set; }

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;

            DATABASE_URL = _configuration["Connections:Database"];
            BOT_URL = _configuration["Bot:Url"];
            BOT_USERNAME = _configuration["Bot:Username"];
        }

        public void ConfigureServices(IServiceCollection services)
        {
            UpdateService updateService = new UpdateService();

            services
                .AddDbContext<MafaniaBotDBContext>()
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