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
            if (!_updateEngine.Supported(update)) return Ok();

            if (update.Type == UpdateType.Message && update.Message.Entities != null)
                await _updateEngine.HandleMessage(update);

            else if (update.Type == UpdateType.Message && update.Message.Entities == null)
                await _updateEngine.HandleEvent(update);

            else if (update.Type == UpdateType.CallbackQuery)
                await _updateEngine.HandleCallbackQuery(update);

            else if (update.Type == UpdateType.MyChatMember)
                await _updateEngine.HandleMyChatMember(update);

            return Ok();
        }
    }
}
