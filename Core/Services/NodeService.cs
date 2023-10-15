using Core.Models;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace Core.Services
{
    internal class NodeService : INodeService
    {
        private readonly string _connectionCenterAddress;
        private readonly string _address;
        private readonly List<Block> _blocks = new();
        private readonly object _lck = new();
        private readonly Dictionary<string, (Block block, int ackAmount)> _pendingBlocksMap = new();
        private readonly ILogger<NodeService> _logger;
        private CancellationTokenSource _miningCTS = new();

        public NodeService(IConfiguration configuration, ILogger<NodeService> logger)
        {
            _connectionCenterAddress = configuration.GetRequiredSection("connectionCenterUrl").Value!;
            _address = configuration.GetRequiredSection(WebHostDefaults.ServerUrlsKey).Value!;
            _logger = logger;
        }

        public IEnumerable<Block> GetBlocks()
        {
            return _blocks;
        }

        public async Task MineBlock()
        {
            await Task.Yield();
            _logger.LogInformation("Start mining");
            var lastBlock = _blocks.Last();
            var block = new Block(lastBlock.Hash, DateTime.UtcNow.ToString(), lastBlock.BlockNum + 1, _address);
            block.Hash = block.CalculateHash();
            _miningCTS = new();
            while (!IsBlockValid(block) && !_miningCTS.Token.IsCancellationRequested)
            {
                block.Nonce++;
                block.Hash = block.CalculateHash();
            }

            if (_miningCTS.Token.IsCancellationRequested)
            {
                _logger.LogInformation("Mining stopped");
                return;
            }

            _logger.LogInformation($"Mined a block {block}");
            await ReceiveBlock(block);
        }

        public async Task ReceiveBlock(Block newBlock)
        {
            _logger.LogInformation($"Received a block {newBlock}");
            if (!IsBlockValid(newBlock))
            {
                return;
            }

            if (newBlock.BlockNum != _blocks.Last().BlockNum + 1)
            {
                await SyncBlocks();
            }

            // TODO: start mining when receive
            _miningCTS.Cancel();

            bool isExist;
            (Block block, int ackAmount) res;

            lock (_lck)
            {
                isExist = _pendingBlocksMap.TryGetValue(newBlock.AuthorAddress, out res);
            }
            if (!isExist)
            {
                lock (_lck)
                {
                    var amnt = _blocks.Any(x => x.AuthorAddress == _address) ? 1 : 0;
                    _pendingBlocksMap[newBlock.AuthorAddress] = (newBlock, 1);
                }
                await CommunicateNewBlock(newBlock);
            }
            else
            {
                lock (_lck)
                {
                    _pendingBlocksMap[newBlock.AuthorAddress] = (res.block, res.ackAmount + 1);
                }
            }

            res = _pendingBlocksMap[newBlock.AuthorAddress];

            var decisiveNodesAmount = Math.Ceiling(_blocks.Select(x => x.AuthorAddress).Distinct().Count() / 2f);

            // TODO: add resolving when half <-> half
            if (res.ackAmount >= decisiveNodesAmount)
            {
                lock (_lck)
                {
                    _blocks.Add(newBlock);
                    _pendingBlocksMap.Clear();
                }
                _logger.LogInformation("Added a block");
                _ = MineBlock();
            }
        }

        public async Task NotifyConnectionService()
        {
            var httpClient = new HttpClient();

            var response = await httpClient.PutAsJsonAsync(_connectionCenterAddress + "/add", new Node(_address));
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Notified communication service");
        }

        public async Task SyncBlocks()
        {
            var nodes = await GetNodes();
            var node = nodes.FirstOrDefault(x => x.Address != _address);

            if (node is null)
            {
                if (!_blocks.Any())
                {
                    lock (_lck)
                    {
                        _blocks.Add(new(0, "first block", 0, _address));
                    }
                }
                return;
            }

            var httpClien = new HttpClient();

            var response = await httpClien.GetAsync(node.Address + "/get-blocks");
            response.EnsureSuccessStatusCode();

            var blocks = await response.Content.ReadFromJsonAsync<IEnumerable<Block>>();
            lock (_lck)
            {
                _blocks.Clear();
                _blocks.AddRange(blocks!);
            }
        }

        private async Task CommunicateNewBlock(Block block)
        {
            var nodes = await GetNodes();

            foreach (var node in nodes.Where(x => x.Address != _address && x.Address != block.AuthorAddress))
            {
                var httpClien = new HttpClient();

                _ = httpClien.PostAsJsonAsync(node.Address + "/new-block", block);
            }
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
            return block.Hash % 56121251 == 0;
        }
    }
}
