using Core.Models;

namespace Core.Services.Interfaces
{
    public interface IBlockService
    {
        bool IsBlockValid(Block block);

        int CalculateHash(Block block);

        Block GetGenesisBlock();
    }
}
