using FreeMarketOne.ServerCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FreeMarketOne.WebApi.Controllers
{
    public abstract class BaseController : Controller
    {
        protected ILogger<PeerController> Logger;
        protected FreeMarketOneServer FmOneServer;

        public BaseController(
            ILogger<PeerController> logger,
            FreeMarketOneServer fmOneServer
        )
        {
            Logger = logger;
            FmOneServer = fmOneServer;
        }
    }
}