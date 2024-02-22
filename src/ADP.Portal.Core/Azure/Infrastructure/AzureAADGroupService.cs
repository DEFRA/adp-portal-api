using Microsoft.Graph;


namespace ADP.Portal.Core.Azure.Infrastructure
{
    public class AzureAADGroupService : IAzureAADGroupService
    {
        private readonly GraphServiceClient graphServiceClient;

        public AzureAADGroupService(GraphServiceClient graphServiceClient)
        {
            this.graphServiceClient = graphServiceClient;
        }

        public async Task<bool> AddToAADGroupAsync(Guid groupId, string userPrincipalName)
        {
            return true;
        }
    }
}
