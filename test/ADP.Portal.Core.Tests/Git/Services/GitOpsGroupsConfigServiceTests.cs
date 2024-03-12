﻿using ADP.Portal.Core.Azure.Entities;
using ADP.Portal.Core.Azure.Services;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using NUnit.Framework;
using Octokit;
using System.Net;

namespace ADP.Portal.Core.Tests.Git.Services
{
    [TestFixture]
    public class GitOpsGroupsConfigServiceTests
    {
        private readonly IGitOpsConfigRepository gitOpsConfigRepositoryMock;
        private readonly GitOpsGroupsConfigService gitOpsConfigService;
        private readonly ILogger<GitOpsGroupsConfigService> loggerMock;
        private readonly IGroupService groupServiceMock;
        private readonly Fixture fixture;
        private readonly GitRepo gitRepo;
        public GitOpsGroupsConfigServiceTests()
        {
            gitOpsConfigRepositoryMock = Substitute.For<IGitOpsConfigRepository>();
            loggerMock = Substitute.For<ILogger<GitOpsGroupsConfigService>>();
            groupServiceMock = Substitute.For<IGroupService>();
            gitOpsConfigService = new GitOpsGroupsConfigService(gitOpsConfigRepositoryMock, loggerMock, groupServiceMock);
            fixture = new Fixture();
            gitRepo = fixture.Build<GitRepo>().With(i => i.BranchName, "main").With(i => i.Organisation, "defra").With(i => i.Name, "test").Create();
        }


        [Test]
        public async Task SyncGroupsAsync_GroupsConfigIsNull_ReturnsEmptyResult()
        {
            // Arrange
            GroupsRoot? groupsRoot = null;
            gitOpsConfigRepositoryMock.GetConfigAsync<GroupsRoot>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(groupsRoot);



            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.AccessGroup, gitRepo);

            // Assert
            Assert.That(result.Errors, Is.Empty);
        }


        [Test]
        public async Task SyncGroupsAsync_Returns_Success_WhenOpenVpn_Members_Synced()
        {
            // Arrange
            var groupsRoot = new GroupsRoot
            {
                Groups = [
                  new() { DisplayName = "group1" , Type = GroupType.OpenVpnGroup,  Members= ["vpnuser@test.com"] }
              ]
            };

            var groupId = "openVpngroupId";
            var memberId = "memberId";
            var exstingMemberToberemoved = fixture.Build<AadGroupMember>().CreateMany(1).ToList();
            gitOpsConfigRepositoryMock.GetConfigAsync<GroupsRoot>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(groupsRoot);
            groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns(groupId);
            groupServiceMock.GetUserTypeGroupMembersAsync(Arg.Any<string>()).Returns(exstingMemberToberemoved);
            groupServiceMock.GetUserIdAsync(Arg.Is(groupsRoot.Groups[0].Members[0].ToString())).Returns(memberId);

            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.OpenVpnGroup, gitRepo);

            // Assert
            Assert.That(result.Errors, Is.Empty);
            await groupServiceMock.Received().RemoveGroupMemberAsync(Arg.Is(groupId), Arg.Is(exstingMemberToberemoved[0].Id));
            await groupServiceMock.Received().AddGroupMemberAsync(Arg.Is(groupId), Arg.Is(memberId));
        }

        [Test]
        public async Task SyncGroupsAsync_Returns_Success_WhenOpenVpn_NoMembers()
        {
            // Arrange
            var groupsRoot = new GroupsRoot
            {
                Groups = [
                  new() {  DisplayName = "group1" , Type = GroupType.OpenVpnGroup}
              ]
            };

            var groupId = "openNoMenbersVpnGroupId";
            var memberId = "memberId";
            gitOpsConfigRepositoryMock.GetConfigAsync<GroupsRoot>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(groupsRoot);
            groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns(groupId);
            groupServiceMock.GetUserTypeGroupMembersAsync(Arg.Any<string>()).Returns([]);


            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.OpenVpnGroup, gitRepo);

            // Assert
            Assert.That(result.Errors, Is.Empty);
            await groupServiceMock.DidNotReceive().RemoveGroupMemberAsync(Arg.Is(groupId), Arg.Any<string>());
            await groupServiceMock.DidNotReceive().AddGroupMemberAsync(Arg.Is(groupId), Arg.Is(memberId));
        }


        [Test]
        public async Task SyncGroupsAsync_Returns_Success_WhenUserGroup_Members_Synced()
        {
            // Arrange
            var groupsRoot = new GroupsRoot
            {
                Groups = [
                  new() { DisplayName = "user-group1" , Type = GroupType.UserGroup, Members = ["user@test.com"] }
              ]
            };

            var groupId = "usergroupId";
            var memberId = "memberId";
            var exstingMemberToberemoved = fixture.Build<AadGroupMember>().CreateMany(1).ToList();
            gitOpsConfigRepositoryMock.GetConfigAsync<GroupsRoot>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(groupsRoot);
            groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns(groupId);
            groupServiceMock.GetUserTypeGroupMembersAsync(Arg.Any<string>()).Returns(exstingMemberToberemoved);
            groupServiceMock.GetUserIdAsync(Arg.Is(groupsRoot.Groups[0].Members[0].ToString())).Returns(memberId);
            groupServiceMock.GetGroupMemberShipsAsync(Arg.Is(groupId)).Returns([]);

            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.UserGroup, gitRepo);

            // Assert
            Assert.That(result.Errors, Is.Empty);
            await groupServiceMock.Received().RemoveGroupMemberAsync(Arg.Is(groupId), Arg.Is(exstingMemberToberemoved[0].Id));
            await groupServiceMock.Received().AddGroupMemberAsync(Arg.Is(groupId), Arg.Is(memberId));
        }


        [Test]
        public async Task SyncGroupsAsync_Returns_Success_WhenUserGroup_Memberships_Synced()
        {
            // Arrange
            var groupsRoot = new GroupsRoot
            {
                Groups = [ new() {
                    DisplayName = "user-group-memberships" , Type = GroupType.UserGroup,
                      GroupMemberships = ["member-ship-group"] }
                ]
            };

            var groupId = "userGroupMembershipsId";
            var exstingMembershipsToberemoved = fixture.Build<AadGroup>().CreateMany(1).ToList();

            gitOpsConfigRepositoryMock.GetConfigAsync<GroupsRoot>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(groupsRoot);
            groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns(groupId);
            groupServiceMock.GetUserTypeGroupMembersAsync(Arg.Any<string>()).Returns([]);
            groupServiceMock.GetGroupMemberShipsAsync(Arg.Is(groupId)).Returns(exstingMembershipsToberemoved);

            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.UserGroup, gitRepo);

            // Assert
            Assert.That(result.Errors, Is.Empty);
            await groupServiceMock.Received().RemoveGroupMemberAsync(Arg.Any<string>(), Arg.Is(groupId));
            await groupServiceMock.Received().AddGroupMemberAsync(Arg.Is(groupId), Arg.Any<string>());
        }

        [Test]
        public async Task SyncGroupsAsync_Returns_Success_WhenAccessGroup_GroupMembers_Synced()
        {
            // Arrange
            var groupsRoot = new GroupsRoot
            {
                Groups = [ new() {
                    DisplayName = "access-group-memberships" , Type = GroupType.AccessGroup,
                    Members = ["group-member"]  }
                ]
            };

            var groupId = "accessGroupMemberId";
            var exstingMembersToberemoved = fixture.Build<AadGroupMember>().CreateMany(1).ToList();

            gitOpsConfigRepositoryMock.GetConfigAsync<GroupsRoot>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(groupsRoot);
            groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns(groupId);
            groupServiceMock.GetGroupTypeGroupMembersAsync(Arg.Any<string>()).Returns(exstingMembersToberemoved);


            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.AccessGroup, gitRepo);

            // Assert
            Assert.That(result.Errors, Is.Empty);
            await groupServiceMock.Received().RemoveGroupMemberAsync(Arg.Is(groupId), Arg.Any<string>());
            await groupServiceMock.Received().AddGroupMemberAsync(Arg.Is(groupId), Arg.Any<string>());
        }

        [Test]
        public async Task SyncGroupsAsync_ErrorOccursWhileCreating_UserGroup_ReturnsErrorResult()
        {
            // Arrange
            var groupsRoot = new GroupsRoot
            {
                Groups = [
                   new() { DisplayName = "group1" , Type= GroupType.UserGroup }
               ]
            };

            gitOpsConfigRepositoryMock.GetConfigAsync<GroupsRoot>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(groupsRoot);
            groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns("");

            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.UserGroup, gitRepo);

            // Assert
            Assert.That(result.Errors, Is.Not.Empty);
        }



        [Test]
        public async Task SyncGroupsAsync_ErrorOccursWhileAdding_UserTypeMembers_ReturnsErrorResult()
        {
            // Arrange

            var groupsRoot = new GroupsRoot
            {
                Groups = [
                   new Group() { DisplayName = "group1", Type = GroupType.UserGroup, Members = ["test@test"]  }
               ]
            };
            var groupId = "groupId";
            gitOpsConfigRepositoryMock.GetConfigAsync<GroupsRoot>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(groupsRoot);
            groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns(groupId);
            groupServiceMock.GetUserTypeGroupMembersAsync(Arg.Any<string>()).Returns([]);
            groupServiceMock.GetUserIdAsync(Arg.Any<string>()).Returns((string?)null);
            groupServiceMock.GetGroupMemberShipsAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.UserGroup, gitRepo);

            // Assert
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors[0], Is.EqualTo($"User '{groupsRoot.Groups[0].Members[0]}' not found for the group:{groupsRoot.Groups[0].DisplayName}."));
        }


        [Test]
        public async Task SyncGroupsAsync_ErrorOccursWhileCreating_AccessGroup_ReturnsErrorResult()
        {
            // Arrange
            var groupsRoot = new GroupsRoot
            {
                Groups = [
                   new() { DisplayName = "group1", Type = GroupType.AccessGroup }
               ]
            };

            gitOpsConfigRepositoryMock.GetConfigAsync<GroupsRoot>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(groupsRoot);
            groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns("");
            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.AccessGroup, gitRepo);

            // Assert
            Assert.That(result.Errors, Is.Not.Empty);
        }

        [Test]
        public async Task SyncGroupsAsync_ErrorOccursWhileAdding_GroupTypeMembers_ReturnsErrorResult()
        {
            // Arrange
            var groupsRoot = new GroupsRoot
            {
                Groups = [
                   new Group() { DisplayName = "group1", Type = GroupType.AccessGroup, Members = ["test-group"]  }
               ]
            };

            var groupId = "groupId";

            gitOpsConfigRepositoryMock.GetConfigAsync<GroupsRoot>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(groupsRoot);
            groupServiceMock.GetGroupIdAsync(Arg.Is(groupsRoot.Groups[0].DisplayName)).Returns(groupId);
            groupServiceMock.GetGroupTypeGroupMembersAsync(Arg.Any<string>()).Returns([]);
            groupServiceMock.GetUserIdAsync(Arg.Any<string>()).Returns((string?)null);
            groupServiceMock.GetGroupIdAsync(Arg.Is(groupsRoot.Groups[0].Members[0].ToString())).Returns((string?)null);

            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.AccessGroup, gitRepo);

            // Assert
            Assert.That(result.Errors, Is.Not.Empty);

            Assert.That(result.Errors[0], Is.EqualTo($"Group '{groupsRoot.Groups[0].Members[0].ToString()}' not found."));
        }
    }
}
