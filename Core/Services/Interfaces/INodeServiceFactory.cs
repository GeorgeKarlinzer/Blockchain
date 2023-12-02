namespace Core.Services.Interfaces
{
    public interface INodeServiceFactory
    {
        INodeService GetOrCreateNodeService(string address);
    }
}
