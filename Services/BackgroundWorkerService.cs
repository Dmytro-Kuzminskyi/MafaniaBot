using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using StackExchange.Redis;
using Telegram.Bot;

namespace MafaniaBot.Services
{  
    public class BackgroundWorkerService : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly StdSchedulerFactory stdSchedulerFactory;
        private IScheduler _scheduler;

        public BackgroundWorkerService(IConfiguration configuration, ITelegramBotClient telegramBotClient, IConnectionMultiplexer connectionMultiplexer)
        {
            stdSchedulerFactory = new StdSchedulerFactory();
            _configuration = configuration;
            _telegramBotClient = telegramBotClient;
            _connectionMultiplexer = connectionMultiplexer;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _scheduler?.Shutdown(cancellationToken);
        }

        private async Task ScheduleJob<T>(JobDataMap jobDataMap, string cronExpresssion, CancellationToken cancellationToken) where T : IJob
        {
            _scheduler = await stdSchedulerFactory.GetScheduler(cancellationToken);
            await _scheduler.Start(cancellationToken);

            var jobDetail = JobBuilder.Create<T>()
                                        .WithIdentity(typeof(T).Name)
                                        .UsingJobData(jobDataMap)
                                        .Build();

            var trigger = TriggerBuilder.Create()
                                        .StartNow()
                                        .WithCronSchedule(cronExpresssion)
                                        .Build();

            await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
        }
    }
}
