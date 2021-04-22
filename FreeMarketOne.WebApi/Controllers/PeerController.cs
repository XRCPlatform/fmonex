using FreeMarketOne.ServerCore;
using FreeMarketOne.WebApi.Models;
using Libplanet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FreeMarketOne.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PeerController : BaseController
    {
        public PeerController(ILogger<PeerController> logger, FreeMarketOneServer fmOneServer) : base(logger, fmOneServer)
        {
        }
        
        [HttpGet]
        [Route("")]
        public IActionResult GetInfo()
        {
            // TODO: Add OnionHiddenService, ConnectedBasePeers, ConnectedMarketPeers
            return Json(new InfoModel
            {
                // DataDir = "", // _fmone.DataDir,
                OnionSeedPeers = FmOneServer.OnionSeedsManager.OnionSeedPeers,
                PublicKey = ByteUtil.Hex(FmOneServer.UserManager.GetCurrentUserPublicKey())
            });
        }
    }
}