using Core.Models;
using Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Core.Services
{
    internal class NodeService : INodeService
    {
        private readonly List<Block> _blocks = new();
        private readonly HashSet<Node> _nodes = new();
        private readonly object _lck = new();
        private readonly Dictionary<string, (Block block, int ackAmount)> _pendingBlocksMap = new();
        private readonly HashSet<string> _pengingAckSourcesSet = new();
        private readonly INodeCommunicator _nodeCommunicator;
        private readonly IBlockService _blockService;
        private readonly ILogger<NodeService> _logger;
        private CancellationTokenSource _miningCTS = new();
        private Task _syncBlockTask = default!;

        public Node Node { get; }

        public NodeService(string address, INodeCommunicator nodeCommunicator, IBlockService blockService, ILogger<NodeService> logger)
        {
            _nodeCommunicator = nodeCommunicator;
            _blockService = blockService;
            Node = new(address);
            _logger = logger;
        }

        public IEnumerable<Block> GetBlocks()
        {
            return _blocks;
        }

        public IEnumerable<Node> GetNodes()
        {
            return _nodes;
        }

        public async Task MineBlockAsync()
        {
            await Task.Yield();

            while (true)
            {
                while (_miningCTS.Token.IsCancellationRequested)
                { }
                _logger.LogInformation("Start mining");

                var lastBlock = _blocks.Last();
                var block = new Block(lastBlock.Hash, DateTime.UtcNow.ToString(), lastBlock.BlockNum + 1, Node.Address);
                block.Hash = _blockService.CalculateHash(block);
                _miningCTS = new();

                while (!_blockService.IsBlockValid(block) && !_miningCTS.Token.IsCancellationRequested)
                {
                    block.Nonce++;
                    block.Hash = _blockService.CalculateHash(block);
                }

                if (_miningCTS.Token.IsCancellationRequested)
                {
                    _logger.LogInformation("Mining stopped");
                    continue;
                }

                _logger.LogInformation($"Mined a block {block}");
                await ReceiveBlock(Node, block);
            }
        }

        public async Task ReceiveBlock(Node source, Block newBlock)
        {
            lock (_lck)
            {
                if (!_nodes.Any(x => x.Address == source.Address))
                {
                    _nodes.Add(source);
                }

                _logger.LogInformation($"{source.Address} sent a block {newBlock}");
                if (!_blockService.IsBlockValid(newBlock))
                {
                    return;
                }

                if (source.Address == Node.Address && _pendingBlocksMap.Count > 1)
                {
                    return;
                }

                if (!_pengingAckSourcesSet.Add($"{newBlock}:{source.Address}"))
                {
                    return;
                }

                if (newBlock.BlockNum != _blocks.Last().BlockNum + 1)
                {
                    return;
                }
            }

            _miningCTS.Cancel();

            if (!_pendingBlocksMap.TryGetValue(newBlock.ToString(), out var res))
            {
                lock (_lck)
                {
                    _pendingBlocksMap[newBlock.ToString()] = (newBlock, 1);
                }
                _logger.LogInformation($"Propagating the block...");
                if (!_pendingBlocksMap.Any())
                {
                    await CommunicateNewBlock(newBlock);
                }
            }
            else
            {
                lock (_lck)
                {
                    _pendingBlocksMap[newBlock.ToString()] = (res.block, res.ackAmount + 1);
                }
            }

            lock (_lck)
            {
                if (!_pendingBlocksMap.Any())
                {
                    _pengingAckSourcesSet.Clear();
                    _pendingBlocksMap.Clear();
                    return;
                }

                var maxAck = _pendingBlocksMap.MaxBy(x => x.Value.ackAmount).Value.ackAmount;
                var sumAck = _pendingBlocksMap.Values.Sum(x => x.ackAmount);

                var biggestBlocks = _pendingBlocksMap.Values.Where(x => x.ackAmount == maxAck);
                var nextBiggestAck = _pendingBlocksMap.Values
                    .Select(x => (int?)x.ackAmount)
                    .OrderByDescending(x => x)
                    .FirstOrDefault(x => x < maxAck) ?? 0;

                if (biggestBlocks.First().ackAmount > nextBiggestAck + _nodes.Count - sumAck)
                {
                    var b = biggestBlocks.MinBy(x => x.block.Hash).block;

                    _blocks.Add(b);
                    _pendingBlocksMap.Clear();
                    _pengingAckSourcesSet.Clear();
                    _logger.LogInformation($"Added a block {b}");
                    _miningCTS = new();
                }
            }
        }

        public async Task ConnectToBlockchainAsync(Node? node)
        {
            _nodes.Add(Node);
            if (node is null)
            {
                return;
            }

            var nodes = await _nodeCommunicator.GetNodesAsync(node);

            foreach (var n in nodes)
            {
                _nodes.Add(n);
                await _nodeCommunicator.AddNodeAsync(n, Node);
            }
        }

        public void AddNode(Node node)
        {
            _nodes.Add(node);
        }

        public async Task SyncBlocks()
        {
            _blocks.Clear();
            _blocks.Add(_blockService.GetGenesisBlock());

            if (_nodes.Count == 1)
            {
                return;
            }

            var index = 1;
            var nodesEnumerator = _nodes.GetEnumerator();
            nodesEnumerator.MoveNext();

            while (nodesEnumerator.Current is not null)
            {
                if (nodesEnumerator.Current.Address == Node.Address)
                {
                    nodesEnumerator.MoveNext();
                    continue;
                }

                var block = await _nodeCommunicator.GetBlockAsync(nodesEnumerator.Current, index);

                if (block is null)
                {
                    nodesEnumerator.MoveNext();
                    continue;
                }

                if (!_blockService.IsBlockValid(block))
                {
                    nodesEnumerator.MoveNext();
                    continue;
                }


                if (block.PrevHash == _blocks.Last().Hash)
                {
                    index++;
                    _blocks.Add(block);
                    continue;
                }

                var syncIndex = index;
                var blocksToSync = new List<Block>
                {
                    block
                };

                while (true)
                {
                    var prevBlock = await _nodeCommunicator.GetBlockAsync(nodesEnumerator.Current, --syncIndex);

                    if (prevBlock is null)
                    {
                        nodesEnumerator.MoveNext();
                        break;
                    }

                    if (!_blockService.IsBlockValid(prevBlock) || prevBlock.Hash != blocksToSync.Last().PrevHash)
                    {
                        nodesEnumerator.MoveNext();
                        break;
                    }

                    if (_blocks.FirstOrDefault(x => x.BlockNum == prevBlock.BlockNum)?.ToString() == prevBlock.ToString())
                    {
                        var blocksToRemove = _blocks.Where(x => x.BlockNum > prevBlock.BlockNum).ToList();
                        foreach (var b in blocksToRemove)
                        {
                            _blocks.Remove(b);
                        }
                        index++;
                        _blocks.AddRange(blocksToSync);
                        break;
                    }

                    blocksToSync.Add(prevBlock);
                }
            }
        }

        private async Task CommunicateNewBlock(Block block)
        {
            foreach (var node in _nodes)
            {
                await _nodeCommunicator.AddBlockAsync(Node, node, block);
            }
        }
    }
}
