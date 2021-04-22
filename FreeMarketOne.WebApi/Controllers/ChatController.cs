using FreeMarketOne.ServerCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FreeMarketOne.WebApi.Controllers
{
    [ApiController]
    [Route("api/{controller}")]
    public class ChatController : BaseController
    {
        public ChatController(ILogger<PeerController> logger, FreeMarketOneServer fmOneServer) : base(logger, fmOneServer)
        {
        }

        // GET
        public IActionResult Index()
        {
            return Json("ok");
        }
    }
}