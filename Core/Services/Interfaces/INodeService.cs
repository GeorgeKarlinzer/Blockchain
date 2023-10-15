using Core.Models;

namespace Core.Services.Interfaces
{
    public interface INodeService
    {
        IEnumerable<Block> GetBlocks();
        Task MineBlock();
        Task ReceiveBlock(Block block);
        Task NotifyConnectionService();
    }
}
