using FreeMarketOne.ServerCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FreeMarketOne.WebApi.Controllers
{
    [ApiController]
    [Route("api/{controller}")]
    public class UserController : BaseController
    {
        public UserController(ILogger<PeerController> logger, FreeMarketOneServer fmOneServer) : base(logger, fmOneServer)
        {
        }
        
        /**
         * <summary>
         * Gets all information about the user for this instance.
         * If it is not logged in, it will throw an error stating that the user is not logged in.
         * </summary>
         */
        [Route("")]
        [HttpGet]
        public IActionResult Index()
        {
            return Json("ok");
        }

        /**
         * <summary>
         * Generate the user for the instance.
         * </summary>
         */
        [Route("create")]
        [HttpPost]
        public IActionResult Create()
        {
            return Json("ok");
        }

        /**
         * <summary>
         * Updates information for the user.
         * </summary>
         */
        [Route("update")]
        [HttpPut]
        public IActionResult Update()
        {
            return Json("ok");
        }

        /**
         * <summary>
         * Generate a random seed when creating a user.
         * </summary>
         */
        [Route("randomSeed")]
        [HttpGet]
        public IActionResult GetRandomSeed()
        {
            return Json("ok");
        }

        /**
         * <summary>
         * Login the user. This provides the details to boot the server.
         * Should first if the server is running first.
         * </summary>
         */
        [Route("login")]
        [HttpPost]
        public IActionResult Login()
        {
            return Json("ok");
        }
    }
}