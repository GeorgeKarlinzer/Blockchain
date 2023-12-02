using Core.Models;
using Core.Services.Interfaces;

namespace Core.Tests.Implementations
{
    internal class InMemoryNodesCommunicator : INodeCommunicator
    {
        public INodeServiceFactory Factory { get; set; } = default!;

        public async Task AddBlockAsync(Node source, Node target, Block block)
        {
            await Factory.GetOrCreateNodeService(target.Address).ReceiveBlock(source, block);
        }

        public Task AddNodeAsync(Node target, Node source)
        {
            Factory.GetOrCreateNodeService(target.Address).AddNode(source);
            return Task.CompletedTask;
        }

        public Task<Block?> GetBlockAsync(Node node, int index)
        {
            var block = Factory.GetOrCreateNodeService(node.Address)
                .GetBlocks()
                .FirstOrDefault(x => x.BlockNum == index);

            return Task.FromResult(block);
        }

        public Task<IEnumerable<Node>> GetNodesAsync(Node node)
        {
            return Task.FromResult(Factory.GetOrCreateNodeService(node.Address).GetNodes().ToList().AsEnumerable());
        }
    }
}
