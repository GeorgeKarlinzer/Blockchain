using Core.Services;
using Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Core.Tests")]
namespace Core
{
    public static class Extensions
    {
        public static IServiceCollection AddCore(this IServiceCollection services) =>
            services.AddSingleton<INodeServiceFactory, NodeServiceFactory>()
                    .AddScoped<INodeCommunicator, NodeCommunicator>()
                    .AddScoped<IBlockService, BlockService>();
    }
}
