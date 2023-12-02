using Core.Models;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace NodeHost.Controllers
{
    [ApiController]
    [Route("/")]
    public class NodeController : ControllerBase
    {
        private readonly INodeService _nodeService;

        public NodeController(IConfiguration configuration, INodeServiceFactory factory)
        {
            var address = configuration.GetRequiredSection(WebHostDefaults.ServerUrlsKey).Value!;
            _nodeService = factory.GetOrCreateNodeService(address);
        }

        [HttpGet]
        [Route("/get-blocks")]
        public ActionResult<IEnumerable<Block>> GetBlocks()
        {
            return Ok(_nodeService.GetBlocks());
        }

        [HttpGet]
        [Route("/get-block/{index}")]
        public ActionResult<Block?> GetBlock(int index)
        {
            var block = _nodeService.GetBlocks().FirstOrDefault(x => x.BlockNum == index);
            return Ok(block);
        }

        [HttpPost]
        [Route("/add-block/{address}")]
        public async Task<ActionResult> ReceiveNewBlock(string address, Block block)
        {
            var addr = HttpUtility.UrlDecode(address);
            await _nodeService.ReceiveBlock(new(addr), block);
            return Ok();
        }

        [HttpGet]
        [Route("/get-nodes")]
        public ActionResult<IEnumerable<Node>> GetNodes()
        {
            return Ok(_nodeService.GetNodes());
        }

        [HttpPost]
        [Route("/add-node")]
        public ActionResult AddNode(Node node)
        {
            _nodeService.AddNode(node);
            return Ok();
        }
    }
}