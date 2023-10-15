using Core.Services;
using Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Core
{
    public static class Extensions
    {
        public static IServiceCollection AddCore(this IServiceCollection services) =>
            services.AddSingleton<INodeService, NodeService>();
    }
}
