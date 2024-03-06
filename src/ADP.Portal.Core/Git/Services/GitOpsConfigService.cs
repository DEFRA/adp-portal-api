using ADP.Portal.Core.Azure.Entities;
using ADP.Portal.Core.Azure.Services;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using Mapster;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Text.RegularExpressions;

namespace ADP.Portal.Core.Git.Services
{
    public partial class GitOpsConfigService : IGitOpsConfigService
    {
        private readonly IGitOpsConfigRepository gitOpsConfigRepository;
        private readonly ILogger<GitOpsConfigService> logger;
        private readonly IGroupService groupService;

        [GeneratedRegex("(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])")]
        private static partial Regex KebabCaseRegex();

        public GitOpsConfigService(IGitOpsConfigRepository gitOpsConfigRepository, ILogger<GitOpsConfigService> logger, IGroupService groupService)
        {
            this.gitOpsConfigRepository = gitOpsConfigRepository;
            this.logger = logger;
            this.groupService = groupService;
        }

        public async Task<bool> IsConfigExistsAsync(string teamName, ConfigType configType, GitRepo gitRepo)
        {
            var fileName = GetFileName(teamName, configType);
            try
            {
                var result = await gitOpsConfigRepository.GetConfigAsync<string>(fileName, gitRepo);
                return !string.IsNullOrEmpty(result);
            }
            catch (NotFoundException)
            {
                return false;
            }
        }

        public async Task<GroupSyncResult> SyncGroupsAsync(string teamName, string ownerId, ConfigType configType, GitRepo gitRepo)
        {
            var result = new GroupSyncResult();

            var fileName = GetFileName(teamName, configType);

            logger.LogInformation("Getting config({configType}) for the Team({teamName})'", configType.ToString(), teamName);

            var groupsConfig = await gitOpsConfigRepository.GetConfigAsync<GroupsRoot>(fileName, gitRepo);

            if (groupsConfig != null)
            {
                foreach (var group in groupsConfig.Groups)
                {
                    logger.LogInformation("Getting groupId for the group({DisplayName})", group.DisplayName);
                    var groupId = await groupService.GetGroupIdAsync(group.DisplayName);
                    var isNewGroup = false;

                    if (string.IsNullOrEmpty(groupId) && (configType == ConfigType.UserGroupsMembers || configType == ConfigType.AccessGroupsMembers))
                    {
                        logger.LogInformation("Creating a new Group({DisplayName})", group.DisplayName);
                        var aadGroup = group.Adapt<AadGroup>();
                        aadGroup.OwnerId = ownerId;

                        groupId = await groupService.AddGroupAsync(aadGroup);
                        isNewGroup = true;
                    }

                    if (string.IsNullOrEmpty(groupId))
                    {
                        result.Error.Add($"Group '{group.DisplayName}' does not exists.");
                        continue;
                    }

                    logger.LogInformation("Syncing group members for the group({DisplayName})", group.DisplayName);
                    if (configType == ConfigType.OpenVpnMembers || configType == ConfigType.UserGroupsMembers)
                    {
                        await SyncUserTypeMembersAsync(result, group, groupId, isNewGroup);
                    }

                    if (configType == ConfigType.UserGroupsMembers)
                    {
                        logger.LogInformation("Syncing group memberships for the group({DisplayName})", group.DisplayName);
                        await SyncMembershipsAsync(result, group, groupId, isNewGroup);
                    }

                    if (configType == ConfigType.AccessGroupsMembers)
                    {
                        await SyncGroupTypeMembersAsync(result, group, groupId, isNewGroup);
                    }

                }
            }

            return result;
        }

        private async Task SyncUserTypeMembersAsync(GroupSyncResult result, Entities.Group group, string? groupId, bool isNewGroup)
        {
            if (groupId == null)
            {
                return;
            }

            var existingMembers = isNewGroup ? [] : await groupService.GetUserTypeGroupMembersAsync(groupId);

            foreach (var member in existingMembers)
            {
                if (!group.Members.Contains(member.UserPrincipalName, StringComparer.OrdinalIgnoreCase))
                {
                    await groupService.RemoveGroupMemberAsync(groupId, member.Id);
                }
            }

            var existingMemberNames = existingMembers.Select(i => i.UserPrincipalName).ToList();

            foreach (var member in group.Members)
            {
                if (!existingMemberNames.Contains(member, StringComparer.OrdinalIgnoreCase))
                {
                    var memberId = await groupService.GetUserIdAsync(member);

                    if (memberId == null)
                    {
                        result.Error.Add($"User '{member}' not found.");
                    }
                    else
                    {
                        await groupService.AddGroupMemberAsync(groupId, memberId);
                    }
                }
            }
        }

        private async Task SyncGroupTypeMembersAsync(GroupSyncResult result, Entities.Group group, string? groupId, bool isNewGroup)
        {
            if (groupId == null)
            {
                return;
            }

            var existingMembers = isNewGroup ? [] : await groupService.GetGroupTypeGroupMembersAsync(groupId);

            foreach (var member in existingMembers)
            {
                if (!group.Members.Contains(member.DisplayName, StringComparer.OrdinalIgnoreCase))
                {
                    await groupService.RemoveGroupMemberAsync(groupId, member.Id);
                }
            }

            var existingMemberNames = existingMembers.Select(i => i.DisplayName).ToList();

            foreach (var member in group.Members)
            {
                if (!existingMemberNames.Contains(member, StringComparer.OrdinalIgnoreCase))
                {
                    var memberId = await groupService.GetGroupIdAsync(member);

                    if (memberId == null)
                    {
                        result.Error.Add($"Group '{member}' not found.");
                    }
                    else
                    {
                        await groupService.AddGroupMemberAsync(groupId, memberId);
                    }
                }
            }
        }

        private async Task SyncMembershipsAsync(GroupSyncResult result, Entities.Group group, string? groupId, bool IsNewGroup)
        {
            if (groupId == null)
            {
                return;
            }

            var existingMemberShips = IsNewGroup ? [] : await groupService.GetGroupMemberShipsAsync(groupId);


            foreach (var memberShip in existingMemberShips)
            {
                if (memberShip.Id != null && !group.GroupMemberships.Contains(memberShip.DisplayName, StringComparer.OrdinalIgnoreCase))
                {
                    await groupService.RemoveGroupMemberAsync(memberShip.Id, groupId);
                }
            }

            var existingMembershipNames = existingMemberShips.Select(i => i.DisplayName).ToList();

            foreach (var groupMembership in group.GroupMemberships)
            {
                if (!existingMembershipNames.Contains(groupMembership, StringComparer.OrdinalIgnoreCase))
                {
                    var groupMembershipId = await groupService.GetGroupIdAsync(groupMembership);
                    if (groupMembershipId == null)
                    {
                        result.Error.Add($"Membership Group '{groupMembership}' not found.");
                    }
                    else
                    {
                        await groupService.AddGroupMemberAsync(groupMembershipId, groupId);
                    }
                }
            }
        }

        private static string GetFileName(string teamName, ConfigType configType)
        {
            return $"{teamName}/{ToKebabCase(configType.ToString())}.yaml";
        }
        private static string ToKebabCase(string name)
        {
            return KebabCaseRegex().Replace(name, "-$1").ToLower();
        }
    }
}
