using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace MafaniaBot.Controllers
{
    public class WebhookController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromServices] IUpdateService updateService, [FromBody] Update update)
        {
            await updateService.ProcessUpdate(update);

            return Ok();
        }
    }
}
