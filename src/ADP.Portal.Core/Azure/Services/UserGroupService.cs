using ADP.Portal.Core.Azure.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;

namespace ADP.Portal.Core.Azure.Services
{
    public class UserGroupService : IUserGroupService
    {
        private readonly ILogger<UserGroupService> logger;
        private readonly IAzureAADGroupService azureAADGroupService;

        public UserGroupService(IAzureAADGroupService azureAADGroupService, ILogger<UserGroupService> logger)
        {
            this.azureAADGroupService = azureAADGroupService;
            this.logger = logger;
        }
        public async Task<bool> AddUserToGroupAsync(Guid groupId, string userPrincipalName)
        { 
            try
            {
                return await azureAADGroupService.AddToAADGroupAsync(groupId, userPrincipalName);
            }
            catch (ProjectDoesNotExistWithNameException)
            {
                logger.LogWarning("User {userPrincipalName} does not exist", userPrincipalName);
                return false;
            }

        }
    }
}
