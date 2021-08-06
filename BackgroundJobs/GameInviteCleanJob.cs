using System.Threading.Tasks;
using MafaniaBot.Engines; 
using Quartz;

namespace MafaniaBot.BackgroundJobs
{
    [DisallowConcurrentExecution]
    public class GameInviteCleanJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            Logger.Log.Debug($"{GetType().Name}: triggered.");
            await GameEngine.Instance.RemoveExpiredGameInvites();
        }
    }
}
