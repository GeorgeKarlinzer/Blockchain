using Core.Models;
using Core.Services;
using Core.Services.Interfaces;
using Core.Tests.Implementations;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Reflection;
using static System.Reflection.Metadata.BlobBuilder;

namespace Core.Tests
{
    public class NodeServiceTests
    {
        private INodeServiceFactory _serviceFactory = default!;

        private void Setup()
        {
            var factory = new InMemoryNodesCommunicator();
            var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            var serviceScope = Substitute.For<IServiceScope>();

            serviceScope.ServiceProvider.Returns(serviceProvider);
            serviceScopeFactory.CreateScope().Returns(serviceScope);

            serviceProvider.GetService(typeof(IBlockService)).Returns(new TestBlockService());
            serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(serviceScopeFactory);
            serviceProvider.GetService(typeof(INodeCommunicator)).Returns(factory);
            serviceProvider.GetService(typeof(ILogger<NodeService>)).Returns(new NullLogger<NodeService>());

            serviceProvider.CreateScope()
                .Returns(serviceScope);

            _serviceFactory = new NodeServiceFactory(serviceProvider);
            factory.Factory = _serviceFactory;
        }

        [Fact]
        public async Task ConnectToBlockchain()
        {
            // Arrange
            Setup();
            var node1 = _serviceFactory.GetOrCreateNodeService("node1");
            var node2 = _serviceFactory.GetOrCreateNodeService("node2");
            var node3 = _serviceFactory.GetOrCreateNodeService("node3");
            var node4 = _serviceFactory.GetOrCreateNodeService("node4");
            var node5 = _serviceFactory.GetOrCreateNodeService("node5");

            // Act
            await node1.ConnectToBlockchainAsync(null);
            await node2.ConnectToBlockchainAsync(node1.Node);
            await node3.ConnectToBlockchainAsync(node1.Node);
            await node4.ConnectToBlockchainAsync(node1.Node);
            await node5.ConnectToBlockchainAsync(node3.Node);

            // Assert
            using var scope = new AssertionScope();
            node1.GetNodes().Should().HaveCount(5);
            node2.GetNodes().Should().HaveCount(5);
            node3.GetNodes().Should().HaveCount(5);
            node4.GetNodes().Should().HaveCount(5);
            node5.GetNodes().Should().HaveCount(5);
        }

        [Fact]
        public async Task SyncBlocks()
        {
            // Arrange
            Setup();
            var blockService = new TestBlockService();

            var node1 = _serviceFactory.GetOrCreateNodeService("node1");
            var node2 = _serviceFactory.GetOrCreateNodeService("node2");
            var node3 = _serviceFactory.GetOrCreateNodeService("node3");
            var node4 = _serviceFactory.GetOrCreateNodeService("node4");
            await node1.ConnectToBlockchainAsync(null);
            await node2.ConnectToBlockchainAsync(node1.Node);
            await node3.ConnectToBlockchainAsync(node1.Node);
            await node4.ConnectToBlockchainAsync(node1.Node);
            SetBlocks(node1, blockService.GetGenesisBlock(), new(0, "1", 1, "node1") { Hash = 11 });
            SetBlocks(node2, new(0, "0", 0, "node2") { Hash = 1 }, new(1, "1", 1, "node2") { Hash = 2 }, new(2, "2", 2, "node2"));
            SetBlocks(node3, blockService.GetGenesisBlock(), new(0, "1", 1, "node1") { Hash = 22 }, new(22, "2", 2, "node1") { Hash = 2 }, new(2, "3", 3, "node2"));

            // Act
            await node4.SyncBlocks();

            // Assert  n
            node4.GetBlocks().Should().HaveCount(4);
        }

        [Fact]
        public async Task ReceiveBlock()
        {
            // Arrange
            Setup();
            var node1 = _serviceFactory.GetOrCreateNodeService("node1");
            SetNodes(node1, node1.Node, new("2"), new("3"), new("4"), new("5"));
            SetBlocks(node1, new Block(0, "111", 0, "node1"));

            // Act
            await node1.ReceiveBlock(new("2"), new(0, "222", 1, "1"));
            await node1.ReceiveBlock(new("3"), new(0, "333", 1, "1"));
            await node1.ReceiveBlock(new("4"), new(0, "444", 1, "1"));
            await node1.ReceiveBlock(new("5"), new(0, "555", 1, "1"));

            // Assert
            node1.GetBlocks().Should().HaveCount(2);
        }

        private static void SetBlocks(INodeService nodeService, params Block[] blocks)
        {
            var field = typeof(NodeService).GetField("_blocks", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var nodeBlocks = (List<Block>)field.GetValue(nodeService)!;
            nodeBlocks.AddRange(blocks);
        }

        private static void SetNodes(INodeService nodeService, params Node[] nodes)
        {
            var field = typeof(NodeService).GetField("_nodes", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var nodeNodes = (HashSet<Node>)field.GetValue(nodeService)!;

            foreach (var node in nodes)
            {
                nodeNodes.Add(node);
            }
        }
    }
}