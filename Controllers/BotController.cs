using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
            if (update == null) return Ok();

            if (update.Type == UpdateType.Message && update.Message.Entities != null)
                await _updateEngine.HandleIncomingMessage(update);

            else if (update.Type == UpdateType.Message && update.Message.Entities == null)
                await _updateEngine.HandleIncomingEvent(update);

            else if (update.Type == UpdateType.CallbackQuery)
                await _updateEngine.HandleIncomingCallbackQuery(update);

            return Ok();
        }
    }
}
