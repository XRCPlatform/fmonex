using FreeMarketOne.ServerCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FreeMarketOne.WebApi.Controllers
{
    [ApiController]
    [Route("api/{controller}")]
    public class CurrencyController : BaseController
    {
        public CurrencyController(ILogger<PeerController> logger, FreeMarketOneServer fmOneServer) : base(logger, fmOneServer)
        {
        }
        
        /**
         * <summary>
         * Lookup the current price for a currency pair.
         * </summary>
         *
         * <param name="from"></param>
         * <param name="to"></param>
         */
        [Route("{from}/{to}")]
        [HttpGet]
        public IActionResult Index(string from, string to)
        {
            return Json("ok");
        }
    }
}