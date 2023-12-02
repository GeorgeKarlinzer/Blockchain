using Core.Models;

namespace Core.Services.Interfaces
{
    public interface INodeCommunicator
    {
        public Task<Block?> GetBlockAsync(Node node, int index);
        public Task<IEnumerable<Node>> GetNodesAsync(Node node);
        public Task AddBlockAsync(Node source, Node target, Block block);
        public Task AddNodeAsync(Node target, Node source);
    }
}
