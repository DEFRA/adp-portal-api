namespace ADP.Portal.Core.Azure.Infrastructure
{
    public interface IAzureAADGroupService
    {
        public Task<bool> AddToAADGroupAsync(Guid groupId, string userPrincipalName);
    }
}
