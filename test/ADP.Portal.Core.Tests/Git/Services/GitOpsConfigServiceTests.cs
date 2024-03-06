using ADP.Portal.Core.Azure.Entities;
using ADP.Portal.Core.Azure.Services;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Octokit;
using System.Net;

namespace ADP.Portal.Core.Tests.Git.Services
{
    [TestFixture]
    public class GitOpsConfigServiceTests
    {
        private readonly IGitOpsConfigRepository gitOpsConfigRepositoryMock;
        private readonly GitOpsConfigService gitOpsConfigService;
        private readonly ILogger<GitOpsConfigService> loggerMock;
        private readonly IGroupService groupServiceMock;
        private readonly Fixture fixture;
        public GitOpsConfigServiceTests()
        {
            gitOpsConfigRepositoryMock = Substitute.For<IGitOpsConfigRepository>();
            loggerMock = Substitute.For<ILogger<GitOpsConfigService>>();
            groupServiceMock = Substitute.For<IGroupService>();
            gitOpsConfigService = new GitOpsConfigService(gitOpsConfigRepositoryMock, loggerMock, groupServiceMock);
            fixture = new Fixture();
        }

        [Test]
        public async Task IsConfigExistsAsync_ConfigExists_ReturnsTrue()
        {
            // Arrange
            gitOpsConfigRepositoryMock.GetConfigAsync<string>(Arg.Any<string>(), Arg.Any<GitRepo>())
            .Returns("config");
            var gitRepo = fixture.Build<GitRepo>()
                .With(i => i.BranchName, "main")
                .With(i => i.Organisation, "defra")
                .With(i => i.RepoName, "test")
                .Create();

            // Act
            var result = await gitOpsConfigService.IsConfigExistsAsync("teamName", ConfigType.GroupsMembers, gitRepo);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsConfigExistsAsync_ConfigDoesNotExist_ReturnsFalse()
        {
            // Arrange
            gitOpsConfigRepositoryMock.GetConfigAsync<string>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(string.Empty);

            var gitRepo = fixture.Build<GitRepo>()
               .With(i => i.BranchName, "main")
               .With(i => i.Organisation, "defra")
               .With(i => i.RepoName, "test")
               .Create();

            // Act
            var result = await gitOpsConfigService.IsConfigExistsAsync("teamName", ConfigType.OpenVpnMembers, gitRepo);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsConfigExistsAsync_ErrorOccurs_ReturnsFalse()
        {
            // Arrange
            gitOpsConfigRepositoryMock.GetConfigAsync<string>(Arg.Any<string>(), Arg.Any<GitRepo>()).ThrowsAsync(new NotFoundException("message", HttpStatusCode.NotFound));
            var gitRepo = fixture.Build<GitRepo>()
               .With(i => i.BranchName, "main")
               .With(i => i.Organisation, "defra")
               .With(i => i.RepoName, "test")
               .Create();

            // Act
            var result = await gitOpsConfigService.IsConfigExistsAsync("teamName", ConfigType.GroupsMembers, gitRepo);

            // Assert
            Assert.That(result, Is.False);
        }


        [Test]
        public async Task SyncGroupsAsync_GroupsConfigIsNull_ReturnsEmptyResult()
        {
            // Arrange
            GroupsRoot? groupsRoot = null;
            gitOpsConfigRepositoryMock.GetConfigAsync<GroupsRoot>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(groupsRoot);

            var gitRepo = fixture.Build<GitRepo>()
              .With(i => i.BranchName, "main")
              .With(i => i.Organisation, "defra")
              .With(i => i.RepoName, "test")
              .Create();

            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("teamName", "ownerId", ConfigType.GroupsMembers, gitRepo);

            // Assert
            Assert.That(result.Error, Is.Empty);
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

            var gitRepo = fixture.Build<GitRepo>().With(i => i.BranchName, "main").With(i => i.Organisation, "defra").With(i => i.RepoName, "test").Create();

            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("teamName", "ownerId", ConfigType.GroupsMembers, gitRepo);

            // Assert
            Assert.That(result.Error, Is.Not.Empty);
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

            gitOpsConfigRepositoryMock.GetConfigAsync<GroupsRoot>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(groupsRoot);

            var gitRepo = fixture.Build<GitRepo>().With(i => i.BranchName, "main").With(i => i.Organisation, "defra").With(i => i.RepoName, "test").Create();
            groupServiceMock.GetUserIdAsync(groupsRoot.Groups[0].Members[0]).Returns("");

            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("teamName", "ownerId", ConfigType.GroupsMembers, gitRepo);

            // Assert
            Assert.That(result.Error, Is.Not.Empty);
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

            var gitRepo = fixture.Build<GitRepo>().With(i => i.BranchName, "main").With(i => i.Organisation, "defra").With(i => i.RepoName, "test").Create();

            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("teamName", "ownerId", ConfigType.GroupsMembers, gitRepo);

            // Assert
            Assert.That(result.Error, Is.Not.Empty);
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

            gitOpsConfigRepositoryMock.GetConfigAsync<GroupsRoot>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(groupsRoot);

            var gitRepo = fixture.Build<GitRepo>().With(i => i.BranchName, "main").With(i => i.Organisation, "defra").With(i => i.RepoName, "test").Create();
            groupServiceMock.GetGroupIdAsync(groupsRoot.Groups[0].Members[0]).Returns("");

            // Act
            var result = await gitOpsConfigService.SyncGroupsAsync("teamName", "ownerId", ConfigType.GroupsMembers, gitRepo);

            // Assert
            Assert.That(result.Error, Is.Not.Empty);
        }
    }
}
