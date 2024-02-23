using ADP.Portal.Core.Ado.Services;
using ADP.Portal.Core.Azure.Infrastructure;
using ADP.Portal.Core.Azure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
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
            var expectedUserId = "12345";
            azureAADGroupServicMock.GetUserIdAsync(userPrincipalName).Returns(expectedUserId);

            // Act
            var actualUserId = await userGroupService.GetUserIdAsync(userPrincipalName);

            // Assert
            Assert.That(actualUserId, Is.Not.Null);
        }



        [Test]
        public async Task AddUserToGroupAsync_UserNotExistingMember_AddsUserToGroup()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var userPrincipalName = "test@domain.com";
            var userId = "12345";
            azureAADGroupServicMock.ExsistingMemberAsync(groupId, userPrincipalName).Returns(false);

            // Act
            var result = await userGroupService.AddUserToGroupAsync(groupId, userPrincipalName, userId);

            // Assert
            await azureAADGroupServicMock.Received().AddToAADGroupAsync(groupId, userId);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task AddUserToGroupAsync_UserExistingMember_DoesNotAddUserToGroup()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var userPrincipalName = "test@domain.com";
            var userId = "12345";
            azureAADGroupServicMock.ExsistingMemberAsync(groupId, userPrincipalName).Returns(true);

            // Act
            var result = await userGroupService.AddUserToGroupAsync(groupId, userPrincipalName, userId);

            // Assert
            await azureAADGroupServicMock.DidNotReceive().AddToAADGroupAsync(groupId, userId);
            Assert.That(result, Is.True);
        }

    }
}