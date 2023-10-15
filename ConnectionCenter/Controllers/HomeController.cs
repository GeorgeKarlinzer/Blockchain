using Core.Models;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConnectionCenter.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        public readonly static List<Node> _nodes = new();

        [HttpGet]
        [Route("/get-nodes")]
        public ActionResult<IEnumerable<Node>> GetAllNodes()
        {
            return Ok(_nodes);
        }

        [HttpPut]
        [Route("/add")]
        public ActionResult AddNode(Node node)
        {
            if (!_nodes.Contains(node))
            {
                _nodes.Add(node);
            }
            return Ok();
        }
    }
}