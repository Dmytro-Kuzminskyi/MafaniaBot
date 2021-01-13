using System;
using MafaniaBot.Abstractions;
using MafaniaBot.Services;
using MafaniaBot.Engines;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using MafaniaBot.Models;

namespace MafaniaBot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        public static string Conn { get; private set; }
        private string env;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
            env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Conn = env == "Development" ?
                _configuration["dev:ConnectionString"] : _configuration["prod:ConnectionString"];
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddDbContext<MafaniaBotContext>(options => 
                options.UseSqlServer(Conn))
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