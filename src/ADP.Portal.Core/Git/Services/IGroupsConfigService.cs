﻿using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Services
{
    public interface IGroupsConfigService
    {
        Task<IEnumerable<Group>> GetGroupsConfigAsync(string tenantName, string teamName);

        Task<GroupConfigResult> CreateGroupsConfigAsync(string tenantName, string teamName, IEnumerable<string> adminGroupMembers, IEnumerable<string> techUserGroupMembers, IEnumerable<string> nonTechUserGroupMembers);

        Task<GroupSyncResult> SyncGroupsAsync(string tenantName, string teamName, string ownerId, GroupType? groupType);

        Task<GroupConfigResult> SetGroupMembersAsync(string tenantName, string teamName, IEnumerable<string> adminGroupMembers, IEnumerable<string> techUserGroupMembers, IEnumerable<string> nonTechUserGroupMembers);
    }
}