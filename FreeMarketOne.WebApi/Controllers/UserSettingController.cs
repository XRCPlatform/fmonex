using FreeMarketOne.ServerCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FreeMarketOne.WebApi.Controllers
{
    public class UserSettingController : BaseController
    {
        public UserSettingController(ILogger<PeerController> logger, FreeMarketOneServer fmOneServer) : base(logger, fmOneServer)
        {
        }
        
        // GET
        public IActionResult Index()
        {
            return Json("ok");
        }
    }
}