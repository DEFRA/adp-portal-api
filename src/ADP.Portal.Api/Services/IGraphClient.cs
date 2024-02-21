using Microsoft.Graph;

namespace ADP.Portal.Core.Ado.Services
{
    public interface IGraphClient
    {
        Task<GraphServiceClient> GetServiceClient();
        public string? GetGroupId();
    }
}
