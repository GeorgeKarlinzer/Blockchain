using Core.Models;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace NodeHost.Controllers
{
    [ApiController]
    [Route("/")]
    public class NodeController : ControllerBase
    {
        private readonly INodeService _nodeService;

        public NodeController(INodeService nodeService)
        {
            _nodeService = nodeService;
        }

        [HttpGet]
        [Route("/get-blocks")]
        public ActionResult<IEnumerable<Block>> GetBlocks()
        {
            return Ok(_nodeService.GetBlocks());
        }

        [HttpPost]
        [Route("/new-block")]
        public async Task<ActionResult> ReceiveNewBlock(Block block)
        {
            await _nodeService.ReceiveBlock(block);
            return Ok();
        }
    }
}