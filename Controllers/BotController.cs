using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace MafaniaBot.Controllers
{
    [ApiController]
    [Route("api/message/update")]
    public class BotController : Controller
    {
        private readonly IUpdateEngine _updateEngine;

        public BotController(IUpdateEngine updateEngine)
        {
            _updateEngine = updateEngine;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            await _updateEngine.ProcessUpdate(update);

            return Ok();
        }
    }
}
