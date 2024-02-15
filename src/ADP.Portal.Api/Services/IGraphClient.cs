using Microsoft.Graph;

namespace ADP.Portal.Core.Ado.Services
{
    internal interface IGraphClient
    {
        void ConfigureAzureAD();
        Task<GraphServiceClient> GetServiceClient();
        public string GetGroupObjectId();
    }
}
