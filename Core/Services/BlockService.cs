using Core.Models;
using Core.Services.Interfaces;

namespace Core.Services
{
    internal class BlockService : IBlockService
    {
        public int CalculateHash(Block block)
        {
            return block.PrevHash ^ unchecked((int)(block.Nonce * 127312231)) ^ GetHash(block.Data) ^ unchecked((int)(block.BlockNum * 7658123717)) ^ GetHash(block.AuthorAddress);
        }

        private static int GetHash(string str)
        {
            var i = 1;
            return unchecked(str.ToCharArray().Sum(x => x * i++));
        }

        public Block GetGenesisBlock()
        {
            var block = new Block(0, "First block", 0, "Origin");
            block.Hash = CalculateHash(block);
            return block;
        }

        public bool IsBlockValid(Block block)
        {
            if(block.BlockNum == 0)
            {
                return block.ToString() == GetGenesisBlock().ToString();
            }

            return block.Hash == CalculateHash(block) && block.Hash % 56121251 == 0;
        }
    }
}
