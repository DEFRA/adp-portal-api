
using ADP.Portal.Core.Azure.Infrastructure;

namespace ADP.Portal.Core.Azure.Services
{
    public class UserGroupService : IUserGroupService
    {
        private readonly IAzureAADGroupService azureAADGroupService;

        public UserGroupService(IAzureAADGroupService azureAADGroupService)
        {
            this.azureAADGroupService = azureAADGroupService;
        }
        public async Task<bool> AddUserToGroupAsync(Guid groupId, string userPrincipalName)
        {
            await azureAADGroupService.AddToAADGroupAsync(groupId, userPrincipalName);

            return true;
        }
    }
}
