using Core.Models;
using Core.Services.Interfaces;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Web;

namespace Core.Services
{
    internal class NodeCommunicator : INodeCommunicator
    {
        private readonly HttpClient _httpClient;

        public NodeCommunicator()
        {
            _httpClient = new HttpClient();
        }

        public async Task AddBlockAsync(Node source, Node target, Block block)
        {
            var addr = HttpUtility.UrlEncode(source.Address);
            await _httpClient.PostAsJsonAsync($"{target.Address}/add-block/{addr}", block);
        }

        public async Task AddNodeAsync(Node target, Node source)
        {
            await _httpClient.PostAsJsonAsync(target.Address + "/add-node", source);
        }

        public async Task<Block?> GetBlockAsync(Node node, int index)
        {
            var response = await _httpClient.GetAsync($"{node.Address}/get-block/{index}");
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return default;
            }

            var block = await response.Content.ReadFromJsonAsync<Block>();
            return block;
        }

        public async Task<IEnumerable<Node>> GetNodesAsync(Node node)
        {
            var response = await _httpClient.GetAsync(node.Address + "/get-nodes");
            var nodes = await response!.Content.ReadFromJsonAsync<IEnumerable<Node>>();
            return nodes!;
        }
    }
}
