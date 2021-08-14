using System;
using System.Linq;
using System.Threading.Tasks;
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
                .AddSingleton<IUpdateEngine, UpdateEngine>()
                .AddHostedService<BackgroundWorkerService>()
                .AddControllers()
                .AddNewtonsoftJson(options =>
                    {
                        options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    });
        }

        public void Configure(IApplicationBuilder app, ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            var updateService = UpdateService.Instance;
            var commands = updateService.Commands.Where(e => e.Key.GetType() != typeof(StartCommand));
            var scopes = updateService.Commands.Values.Distinct();

            foreach (var scope in scopes)
            {
                botClient.SetMyCommandsAsync(commands.Where(e => e.Value == scope).Select(e => e.Key), BaseDictionary.BotCommandScopeMap[scope]).Wait();
            }

            SyncronizeRedisData(botClient, redis);

            app
                .UseRouting()
                .UseEndpoints(endpoints =>
                    {
                        endpoints.MapDefaultControllerRoute();
                    });
        }

        private void SyncronizeRedisData(ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            try
            {
                IDatabase db = redis.GetDatabase();

                var chatIds = db.SetMembers("MyGroups").Select(e => long.Parse(e)).ToArray();

                Parallel.ForEach(chatIds, chatId =>
                {
                    var userIds = db.SetMembers($"ChatMembers:{chatId}").Select(e => long.Parse(e)).ToArray();

                    foreach (var userId in userIds)
                    {
                        var chatMember = botClient.GetChatMemberAsync(chatId, userId).GetAwaiter().GetResult();

                        if (chatMember == null)
                            db.SetRemove(new RedisKey($"ChatMembers:{chatId}"), new RedisValue(userId.ToString()));
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: redis database error!", ex);
            }
        }
    }
}
