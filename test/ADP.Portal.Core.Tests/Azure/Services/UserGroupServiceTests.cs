using ADP.Portal.Core.Azure.Infrastructure;
using ADP.Portal.Core.Azure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace ADP.Portal.Core.Tests.Ado.Services
{
    [TestFixture]
    public class UserGroupServiceTests
    {
        private readonly IAzureAadGroupService azureAADGroupServicMock;
        private readonly ILogger<UserGroupService> loggerMock;
        private readonly UserGroupService userGroupService;

        public UserGroupServiceTests()
        {
            azureAADGroupServicMock = Substitute.For<IAzureAadGroupService>();
            loggerMock = Substitute.For<ILogger<UserGroupService>>();
            userGroupService = new UserGroupService(azureAADGroupServicMock, loggerMock);
        }

        [Test]
        public void Constructor_WithValidParameters_SetsUserGroupService()
        {
            // Act
            var userGroupService2 = new UserGroupService(azureAADGroupServicMock, loggerMock);

            // Assert
            Assert.That(userGroupService2, Is.Not.Null);
        }

        [Test]
        public async Task GetUserIdAsync_ReturnsExpectedUserId()
        {
            // Arrange
            var userPrincipalName = "test@domain.com";
            var userId = "12345";
            azureAADGroupServicMock.GetUserIdAsync(userPrincipalName).Returns(userId);

            // Act
            var result = await userGroupService.GetUserIdAsync(userPrincipalName);

            // Assert
            Assert.That(result, Is.Not.Null);
        }


        [Test]
        public async Task GetUserIdAsync_Returns_Null_UserId_When_NotExists()
        {
            // Arrange
            var userPrincipalName = "test@domain.com";
            azureAADGroupServicMock.GetUserIdAsync(userPrincipalName).ThrowsAsync(new ODataError() { ResponseStatusCode = 404 });

            // Act
            var result = await userGroupService.GetUserIdAsync(userPrincipalName);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetUserIdAsync_Throw_Unhandled_Exception()
        {
            // Arrange
            var userPrincipalName = "test@domain.com";
            azureAADGroupServicMock.GetUserIdAsync(userPrincipalName).ThrowsAsync<ODataError>();


            // Assert
            Assert.ThrowsAsync<ODataError>(async () => await userGroupService.GetUserIdAsync(userPrincipalName));
        }

        [Test]
        public async Task AddGroupMemberAsync_Return_True_AddsUserToGroup()
        {
            // Arrange
            var groupId = Guid.NewGuid().ToString();
            var userId = "12345";
            azureAADGroupServicMock.AddGroupMemberAsync(groupId, userId).Returns(true);

            // Act
            var result = await userGroupService.AddGroupMemberAsync(groupId, userId);

            // Assert
            await azureAADGroupServicMock.Received().AddGroupMemberAsync(groupId, userId);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task AddGroupMemberAsync_Return_False_AddsUserToGroup()
        {
            // Arrange
            var groupId = Guid.NewGuid().ToString();
            var userId = "12345";
            azureAADGroupServicMock.AddGroupMemberAsync(groupId, userId).Returns(false);

            // Act
            var result = await userGroupService.AddGroupMemberAsync(groupId, userId);

            // Assert
            await azureAADGroupServicMock.Received().AddGroupMemberAsync(groupId, userId);
            Assert.That(result, Is.False);
        }

    }
}