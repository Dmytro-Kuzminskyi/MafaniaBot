using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot
{
    public class ConfigureBot : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;

        public ConfigureBot(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _services = serviceProvider;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _services.CreateScope();
                var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
                var redis = _services.GetRequiredService<IConnectionMultiplexer>();
                var updateService = _services.GetRequiredService<IUpdateService>();
                var webHookUrl = $"{_configuration["Host"]}/bot{_configuration["Bot:Token"]}";

                await botClient.SetWebhookAsync(
                        url: webHookUrl,
                        cancellationToken: cancellationToken);

                await SetupCommands(botClient, updateService.Commands, cancellationToken);
                await SynchronizeData(botClient, redis);
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: start error!", ex);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _services.CreateScope();
                var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

                await botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: stop error!", ex);
            }
        }

        private BotCommandScope GetBotCommandScope(BotCommandScopeType scopeType)
        {
            return scopeType switch
            {
                BotCommandScopeType.AllPrivateChats => BotCommandScope.AllPrivateChats(),
                BotCommandScopeType.AllGroupChats => BotCommandScope.AllGroupChats(),
                _ => BotCommandScope.Default()
            };
        }

        private async Task SetCommandsByScopeType(ITelegramBotClient botClient, ScopedCommand[] commands, BotCommandScopeType scopeType, CancellationToken cancellationToken = default)
        {
            var scopedComands = commands.Where(e => e.ScopeTypes.Contains(scopeType)).ToArray();

            await botClient.DeleteMyCommandsAsync(
                    scope: GetBotCommandScope(scopeType),
                    cancellationToken: cancellationToken);

            await botClient.SetMyCommandsAsync(
                    commands: scopedComands,
                    scope: GetBotCommandScope(scopeType),
                    cancellationToken: cancellationToken);
        }

        private async Task SetupCommands(ITelegramBotClient botClient, ScopedCommand[] commands, CancellationToken cancellationToken = default)
        {
            await SetCommandsByScopeType(botClient, commands, BotCommandScopeType.Default, cancellationToken);
            await SetCommandsByScopeType(botClient, commands, BotCommandScopeType.AllPrivateChats, cancellationToken);
            await SetCommandsByScopeType(botClient, commands, BotCommandScopeType.AllGroupChats, cancellationToken);
        }

        private async Task SynchronizeData(ITelegramBotClient botClient, IConnectionMultiplexer redis)
        {
            IDatabaseAsync db = redis.GetDatabase();

            var myGroupsResult = (RedisKey[])await db.ExecuteAsync("KEYS", "MyGroup:*");

            foreach (var key in myGroupsResult)
            {
                long chatId = default;

                try
                {
                    chatId = long.Parse(key.ToString().Split(':').LastOrDefault());

                    await botClient.GetChatAsync(chatId);
                }
                catch (ApiRequestException ex)
                {
                    var message = ex.Message;

                    if (message == "Bad Request: chat not found")
                        await db.KeyDeleteAsync($"MyGroup:{chatId}");
                }
            }

            myGroupsResult = (RedisKey[])await db.ExecuteAsync("KEYS", "MyGroup:*");

            foreach (var key in myGroupsResult)
            {
                var chatId = long.Parse(key.ToString().Split(':').LastOrDefault());

                var chatMembersResult = (RedisKey[])await db.ExecuteAsync("KEYS", $"ChatMember:{chatId}:*");

                foreach(var user in chatMembersResult)
                {
                    long userId = default;

                    try
                    {
                        userId = long.Parse(user.ToString().Split(':').LastOrDefault());

                        await botClient.GetChatMemberAsync(chatId, userId);
                    }
                    catch (ApiRequestException ex)
                    {
                        var message = ex.Message;

                        if (message == "Bad Request: user not found")
                            await db.KeyDeleteAsync($"ChatMember:{chatId}:{userId}");
                    }
                }
            }

            var myChatMembersResult = (RedisKey[])await db.ExecuteAsync("KEYS", "MyChatMember:*");

            foreach (var key in myChatMembersResult)
            {
                long chatId = default;

                try
                {
                    chatId = long.Parse(key.ToString().Split(':').LastOrDefault());

                    await botClient.GetChatAsync(chatId);
                }
                catch (ApiRequestException ex)
                {
                    var message = ex.Message;

                    if (message == "Bad Request: chat not found")
                        await db.KeyDeleteAsync($"MyChatMember:{chatId}");
                }
            }
        }
    }
}
