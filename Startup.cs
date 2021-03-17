using System;
using MafaniaBot.Models;
using MafaniaBot.Engines;
using MafaniaBot.Services;
using MafaniaBot.Abstractions;
using Newtonsoft.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MafaniaBot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        public static string DATABASE_URL { get; private set; }
        public static string BOT_URL { get; private set; }

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;

            DATABASE_URL = _configuration["Connections:Database"];
            BOT_URL = _configuration["Bot:Url"];
        }

        public void ConfigureServices(IServiceCollection services)
        {      
            services    
                .AddDbContext<MafaniaBotDBContext>()
                .AddScoped<IUpdateEngine, UpdateEngine>()
                .AddScoped<IUpdateService, UpdateService>()
                .AddTelegramBotClient(_configuration)
                .AddControllers()
                .AddNewtonsoftJson(options =>
                    {
                        options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    })
                .AddFluentValidation();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
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