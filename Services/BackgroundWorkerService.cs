using System;
using System.Threading;
using System.Threading.Tasks;
using MafaniaBot.BackgroundJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;

namespace MafaniaBot.Services
{  
    public class BackgroundWorkerService : BackgroundService
    {
        private readonly IConfiguration _configuration;        
        private readonly StdSchedulerFactory stdSchedulerFactory;
        private IScheduler _scheduler;

        public BackgroundWorkerService(IConfiguration configuration)
        {
            stdSchedulerFactory = new StdSchedulerFactory();
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await ScheduleJob<GameInviteCleanJob>(_configuration["BackgroundWorker:GameInviteCleanJobTriggerTime"], cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                await _scheduler.Shutdown();
        }

        private async Task ScheduleJob<T>(string cronExpresssion, CancellationToken cancellationToken) where T : IJob
        {
            _scheduler = await stdSchedulerFactory.GetScheduler();
            await _scheduler.Start();

            var jobDetail = JobBuilder.Create<T>()
                                        .WithIdentity(typeof(T).Name)
                                        .Build();

            var trigger = TriggerBuilder.Create()
                                        .StartNow()
                                        .WithCronSchedule(cronExpresssion)
                                        .Build();

            await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
        }
    }
}
