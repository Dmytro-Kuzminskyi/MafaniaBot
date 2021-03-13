using System;
using Microsoft.Extensions.Configuration;
using MafaniaBot.Properties;
using MafaniaBot.Abstractions;
using MafaniaBot.Services;
using MafaniaBot.Engines;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using MafaniaBot.Models;

namespace MafaniaBot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        public static string DB_CS { get; private set; }
        private string env;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
            env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (env == "Development")
            {
                DB_CS = Resources.DEV_DB_CS;
            }
            else
			{
                string connUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
                connUrl = connUrl.Replace("mysql://", string.Empty);

                string creds = connUrl.Split('@')[0];
                string dbUser = creds.Split(':')[0];
                string dbPass = creds.Split(':')[1];

                string dbInfo = connUrl.Split('@')[1].Split('?')[0];
                string dbHost = dbInfo.Split('/')[0];
                string dbName = dbInfo.Split('/')[1];

                string connStr = $"server={dbHost};user={dbUser};password={dbPass};database={dbName}";
                DB_CS = connStr;
            }
                
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