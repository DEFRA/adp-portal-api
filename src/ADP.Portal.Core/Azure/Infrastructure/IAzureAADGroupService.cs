
namespace ADP.Portal.Core.Azure.Infrastructure
{
    public interface IAzureAADGroupService
    {
        Task<string?> GetUserIdAsync(string userPrincipalName);

        Task<bool> ExsistingMemberAsync(Guid groupId, string userPrincipalName);

        Task<bool> AddToAADGroupAsync(Guid groupId, string userId);
        
    }
}