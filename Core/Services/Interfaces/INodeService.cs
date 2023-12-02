using Core.Models;
using System.Runtime.InteropServices;

namespace Core.Services.Interfaces
{
    public interface INodeService
    {
        Node Node { get; }

        IEnumerable<Block> GetBlocks();
        IEnumerable<Node> GetNodes();
        Task MineBlockAsync();
        Task SyncBlocks();
        Task ReceiveBlock(Node source, Block block);
        Task ConnectToBlockchainAsync(Node? node);
        void AddNode(Node node);
    }
}
