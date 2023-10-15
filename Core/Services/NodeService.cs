using Core.Models;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace Core.Services
{
    internal class NodeService : INodeService
    {
        private readonly string _connectionCenterAddress;
        private readonly string _address;
        private readonly List<Block> _blocks = new();
        private readonly Dictionary<string, (Block block, int ackAmount)> _pendingBlocksMap = new();

        private CancellationTokenSource _miningCTS = new();

        public NodeService(IConfiguration configuration)
        {
            _connectionCenterAddress = configuration.GetRequiredSection("connectionCenterUrl").Value!;
            _address = configuration.GetRequiredSection(WebHostDefaults.ServerUrlsKey).Value!;
        }

        public IEnumerable<Block> GetBlocks()
        {
            return _blocks;
        }

        public async Task MineBlock()
        {
            await SyncBlocks();
            var lastBlock = _blocks.Last();
            var block = new Block(lastBlock.Hash, DateTime.UtcNow.ToString(), lastBlock.BlockNum + 1, _address);
            _miningCTS = new();
            while (!IsBlockValid(block) && !_miningCTS.Token.IsCancellationRequested)
            {
                block.Nonce++;
            }

            if (_miningCTS.Token.IsCancellationRequested)
            {
                return;
            }

            await ReceiveBlock(block);
        }

        public async Task ReceiveBlock(Block block)
        {
            if (!IsBlockValid(block))
            {
                return;
            }

            // TODO: start mining when receive
            _miningCTS.Cancel();

            if (!_pendingBlocksMap.TryGetValue(block.AuthorAddress, out var res))
            {
                _pendingBlocksMap[block.AuthorAddress] = (block, 1);
                await CommunicateNewBlock(block);
            }
            else
            {
                _pendingBlocksMap[block.AuthorAddress] = (res.block, res.ackAmount++);
            }

            res = _pendingBlocksMap[block.AuthorAddress];

            await SyncBlocks();
            var decisiveNodesAmount = Math.Ceiling(_blocks.Select(x => x.AuthorAddress).Distinct().Count() / 2f);

            // TODO: add resolving when half <-> half
            if (res.ackAmount >= decisiveNodesAmount)
            {
                _blocks.Add(block);
                _ = MineBlock();
            }
        }

        public async Task NotifyConnectionService()
        {
            var httpClient = new HttpClient();

            var response = await httpClient.PutAsJsonAsync(_connectionCenterAddress + "/add", new Node(_address));
            response.EnsureSuccessStatusCode();
        }

        private async Task CommunicateNewBlock(Block block)
        {
            var nodes = await GetNodes();

            foreach (var node in nodes.Where(x => x.Address != _address))
            {
                var httpClien = new HttpClient();

                _ = httpClien.PostAsJsonAsync(node.Address + "/new-block", block);
            }
        }

        private async Task SyncBlocks()
        {
            var nodes = await GetNodes();
            var node = nodes.FirstOrDefault(x => x.Address != _address);

            if (node is null)
            {
                if (!_blocks.Any())
                {
                    _blocks.Add(new(0, "first block", 0, _address));
                }
                return;
            }

            var httpClien = new HttpClient();

            var response = await httpClien.GetAsync(node.Address + "/get-blocks");
            response.EnsureSuccessStatusCode();

            var blocks = await response.Content.ReadFromJsonAsync<IEnumerable<Block>>();
            _blocks.Clear();
            _blocks.AddRange(blocks!);
        }

        private async Task<IEnumerable<Node>> GetNodes()
        {
            var httpClient = new HttpClient();

            var response = await httpClient.GetAsync(_connectionCenterAddress + "/get-nodes");
            response.EnsureSuccessStatusCode();

            var nodes = await response.Content.ReadFromJsonAsync<IEnumerable<Node>>();
            return nodes!;
        }



        private static bool IsBlockValid(Block block)
        {
            return block.Hash % 56123151 == 0;
        }
    }
}
