using Core.Models;
using Core.Services.Interfaces;

namespace Core.Tests.Implementations
{
    internal class TestBlockService : IBlockService
    {
        public int CalculateHash(Block block)
        {
            return 0;
        }

        public Block GetGenesisBlock()
        {
            return new(0, "0", 0, "Origin");
        }

        public bool IsBlockValid(Block block)
        {
            if(block.BlockNum == 0)
            {
                return block.ToString() == GetGenesisBlock().ToString();
            }

            return true;
        }
    }
}
