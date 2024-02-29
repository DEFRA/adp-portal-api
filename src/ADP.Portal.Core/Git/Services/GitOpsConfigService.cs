﻿using ADP.Portal.Core.Azure.Entities;
using ADP.Portal.Core.Azure.Services;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using Mapster;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Text.RegularExpressions;

namespace ADP.Portal.Core.Git.Services
{
    public class GitOpsConfigService : IGitOpsConfigService
    {
        private readonly IGitOpsConfigRepository gitOpsConfigRepository;
        private readonly ILogger<GitOpsConfigService> logger;
        private readonly IUserGroupService userGroupService;

        public GitOpsConfigService(IGitOpsConfigRepository gitOpsConfigRepository, ILogger<GitOpsConfigService> logger, IUserGroupService userGroupService)
        {
            this.gitOpsConfigRepository = gitOpsConfigRepository;
            this.logger = logger;
            this.userGroupService = userGroupService;
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

            var filenName = GetFileName(teamName, configType);

            logger.LogInformation("Getting config({configType}) for the Team({teamName})'", configType.ToString(), teamName);

            var groupsConfig = await gitOpsConfigRepository.GetConfigAsync<GroupsRoot>(filenName, gitRepo);

            if (groupsConfig != null)
            {
                foreach (var group in groupsConfig.Groups)
                {
                    logger.LogInformation("Getting groupId for the group({DisplayName})", group.DisplayName);
                    var groupId = await userGroupService.GetGroupIdAsync(group.DisplayName);
                    var isNewGroup = false;

                    if (!group.ManageMembersOnly && string.IsNullOrEmpty(groupId))
                    {
                        logger.LogInformation("Creating a new Group({})", group.DisplayName);
                        var aadGroup = group.Adapt<AadGroup>();
                        aadGroup.OwnerId = ownerId;

                        groupId = await userGroupService.AddGroupAsync(aadGroup);
                        isNewGroup = true;
                    }

                    if (string.IsNullOrEmpty(groupId))
                    {
                        result.Error.Add($"Group '{group.DisplayName}' does not exists.");
                        continue;
                    }

                    logger.LogInformation("Syncing group members for the group({DisplayName})", group.DisplayName);
                    await SyncMembersAsync(result, group, groupId, isNewGroup);

                    if (!group.ManageMembersOnly)
                    {
                        logger.LogInformation("Syncing group memberships for the group({DisplayName})", group.DisplayName);
                        await SyncMembershipsAsync(result, group, groupId, isNewGroup);
                    }
                }
            }

            return result;
        }

        private async Task SyncMembersAsync(GroupSyncResult result, Entities.Group group, string? groupId, bool isNewGroup)
        {
            if (groupId == null)
            {
                return;
            }

            var existingMembers = new List<AadGroupMember>();
            if (!isNewGroup)
            {
                existingMembers = await userGroupService.GetGroupMembersAsync(groupId);
            }

            if (existingMembers?.Count > 0)
            {
                foreach (var member in existingMembers)
                {
                    if (!group.Members.Contains(member.UserPrincipalName, StringComparer.OrdinalIgnoreCase))
                    {
                        await userGroupService.RemoveGroupMemberAsync(groupId, member.Id);
                    }
                }
            }

            foreach (var member in group.Members)
            {
                if (existingMembers == null || (existingMembers.Select(i => i.UserPrincipalName).Contains(member, StringComparer.OrdinalIgnoreCase) == false))
                {
                    var userid = await userGroupService.GetUserIdAsync(member);

                    if (userid == null)
                    {
                        result.Error.Add($"User '{member}' not found.");
                        continue;
                    }
                    else
                    {
                        await userGroupService.AddGroupMemberAsync(groupId, userid);
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

            var existingMemberShips = new List<AadGroup>();
            if (!IsNewGroup)
            {
                existingMemberShips = await userGroupService.GetGroupMemberShipsAsync(groupId);
            }

            if (existingMemberShips?.Count > 0)
            {
                foreach (var memberShip in existingMemberShips)
                {
                    if (memberShip.Id != null && !group.GroupMemberships.Contains(memberShip.DisplayName, StringComparer.OrdinalIgnoreCase))
                    {
                        await userGroupService.RemoveGroupMemberAsync(memberShip.Id, groupId);
                    }
                }
            }

            foreach (var groupMembership in group.GroupMemberships)
            {
                if (existingMemberShips == null || (existingMemberShips.Select(item => item.DisplayName).Contains(groupMembership, StringComparer.OrdinalIgnoreCase) == false))
                {
                    var groupMembershipId = await userGroupService.GetGroupIdAsync(groupMembership);
                    if (groupMembershipId == null)
                    {
                        result.Error.Add($"Membership Group '{groupMembership}' not found.");
                        continue;
                    }
                    else
                    {
                        await userGroupService.AddGroupMemberAsync(groupMembershipId, groupId);
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
            return Regex.Replace(name, "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", "-$1").ToLower();
        }
    }
}
