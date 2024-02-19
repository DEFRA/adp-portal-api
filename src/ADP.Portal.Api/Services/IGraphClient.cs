using Microsoft.Graph;

namespace ADP.Portal.Core.Ado.Services
{
    internal interface IGraphClient
    {
        Task<GraphServiceClient> GetServiceClient();
        public string GetGroupId();
    }
}
