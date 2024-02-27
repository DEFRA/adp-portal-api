using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Services
{
    public interface IGitOpsConfigService
    {
        bool IsConfigExists(string teamName, ConfigType configTypes, string tenant);
        Task<GroupSyncResult> SyncGroupsAsync(string teamName, string ownerId, ConfigType configType, string tenant);
    }
}