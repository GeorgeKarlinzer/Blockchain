using Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.Services
{
    internal class NodeServiceFactory : INodeServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, INodeService> _nodeServicesMap = new();

        public NodeServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public INodeService GetOrCreateNodeService(string address)
        {
            if(_nodeServicesMap.TryGetValue(address, out var service))
            {
                return service;
            }

            var scope = _serviceProvider.CreateScope();
            var communicator = scope.ServiceProvider.GetRequiredService<INodeCommunicator>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<NodeService>>();
            var blockService = scope.ServiceProvider.GetRequiredService<IBlockService>();
            scope.Dispose();

            var nodeService = new NodeService(address, communicator, blockService, logger);

            return _nodeServicesMap[address] = nodeService;
        }
    }
}
