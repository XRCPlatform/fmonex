using System;
using System.Security.Cryptography;
using FreeMarketOne.BlockChain;
using FreeMarketOne.DataStructure;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Markets;
using FreeMarketOne.ServerCore;
using FreeMarketOne.WebApi.Models;
using Libplanet;
using Libplanet.Action;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FreeMarketOne.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlockController : BaseController
    {
        public BlockController(ILogger<PeerController> logger, FreeMarketOneServer fmOneServer) : base(logger, fmOneServer)
        {
        }
        
        [HttpGet]
        [Route("info/{chain}")]
        public IActionResult GetBaseHeight(string chain)
        {
            CheckChainParameter(chain);
            
            var marketChain = FmOneServer.MarketBlockChainManager.BlockChain;
            var baseChain = FmOneServer.BaseBlockChainManager.BlockChain;

            if (marketChain == null || baseChain == null)
            {
                return BadRequest("Chains are not ready.");
            }
            
            return Json(new
            {
                height = chain == "base" ? baseChain.Count : marketChain.Count,
                difficulty = chain == "base" ? baseChain.Tip.Difficulty : marketChain.Tip.Difficulty,
                genesisHash = chain == "base" ? ByteUtil.Hex(baseChain.Genesis.Hash.ToByteArray()) : ByteUtil.Hex(marketChain.Genesis.Hash.ToByteArray()),
                tipHash = chain == "base" ? ByteUtil.Hex(baseChain.Tip.Hash.ToByteArray()) : ByteUtil.Hex(marketChain.Tip.Hash.ToByteArray()),
            });
        }

        [Route("block/byheight/{chain}/{height}")]
        [HttpGet]
        public IActionResult GetBlockByHeight(string chain, string height)
        {
            CheckChainParameter(chain);
            Guard.NotEmpty(height, nameof(height));
            var parsedHeight = 0;
            
            try
            {
                parsedHeight = int.Parse(height);
            }
            catch (Exception)
            {
                throw new ArgumentException(nameof(height));
            } 
            
            var marketChain = FmOneServer.MarketBlockChainManager.BlockChain;
            var baseChain = FmOneServer.BaseBlockChainManager.BlockChain;

            if (marketChain == null || baseChain == null)
            {
                return BadRequest("Chains are not ready.");
            }
            
            return chain == "base" ? Json(baseChain[parsedHeight]) : Json(marketChain[parsedHeight]);
        }
        
        [Route("block/byhash/{chain}/{hash}")]
        [HttpGet]
        public IActionResult GetBlockByHash(string chain, string hash)
        {
            CheckChainParameter(chain);
            
            var marketChain = FmOneServer.MarketBlockChainManager.BlockChain;
            var baseChain = FmOneServer.BaseBlockChainManager.BlockChain;

            if (marketChain == null || baseChain == null)
            {
                return BadRequest("Chains are not ready.");
            }
            byte[] hashBytes = ByteUtil.ParseHex(hash);
            return chain == "base"
                ? Json(BlockModel<BaseAction>.FromBlock(baseChain[new HashDigest<SHA256>(hashBytes)]))
                : Json(BlockModel<MarketAction>.FromBlock(marketChain[new HashDigest<SHA256>(hashBytes)]));
        }

        private void CheckChainParameter(string chain)
        {
            Guard.NotEmpty(nameof(chain), chain);
            if (chain != "base" && chain != "market")
            {
                throw new ArgumentException($"Chain must be 'base' or 'market', not {chain}");
            }
        }
    }
}