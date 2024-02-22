

namespace ADP.Portal.Core.Azure.Services
{
    public interface IUserGroupService
    {
        public Task<bool> AddUserToGroupAsync(Guid groupId, string userPrincipalName);
    }
}
