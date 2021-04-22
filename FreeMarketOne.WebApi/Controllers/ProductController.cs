using FreeMarketOne.ServerCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FreeMarketOne.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : BaseController
    {
        public ProductController(ILogger<PeerController> logger, FreeMarketOneServer fmOneServer) : base(logger, fmOneServer)
        {
        }
        
        /**
         * <summary>
         * Get all products for a particular category
         * if a search query is provided, search for that instead.
         * </summary>
         */
        [Route("/{category?}")]
        public IActionResult Index(string category)
        {
            return Json("ok");
        }

        /**
         * <summary>
         * Products bought for this particular user instance.
         * </summary>
         */ 
        [Route("bought")]
        public IActionResult Bought()
        {
            return Json("ok");
        }
        
        /**
         * <summary>
         * Products sold from this particular user instance.
         * </summary>
         */
        [Route("sold")]
        public IActionResult Sold()
        {
            return Json("ok");
        }

        /**
         * <summary>
         * Create a new product to publish on the market blockchain.
         * </summary>
         */
        [Route("create")]
        public IActionResult Create()
        {
            return Json("ok");
        }

        [Route("update")]
        public IActionResult Update()
        {
            return Json("ok");
        }
    }
}