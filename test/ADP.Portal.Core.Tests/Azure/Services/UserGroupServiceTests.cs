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
        private readonly IAzureAADGroupService AzureAADGroupServicMock;
        private readonly ILogger<UserGroupService> loggerMock;
        private readonly UserGroupService userGroupService;

        public UserGroupServiceTests()
        {
            AzureAADGroupServicMock = Substitute.For<IAzureAADGroupService>();
            loggerMock = Substitute.For<ILogger<UserGroupService>>();
            userGroupService = new UserGroupService(AzureAADGroupServicMock, loggerMock);
        }

        [Test]
        public void Constructor_WithValidParameters_SetsAzureAADGroupService()
        {
            // Act
            var usrGroupService = new UserGroupService(AzureAADGroupServicMock, loggerMock);

            // Assert
            Assert.That(usrGroupService, Is.Not.Null);
        }

        [Test]
        public async Task AddUserToGroupAsync_ReturnsFalse_WhenUserNotAdded()
        {
            // Arrange
            var userName = "testuser";
            Guid groupId = Guid.NewGuid();

            // Act
            var result = await userGroupService.AddUserToGroupAsync(groupId, userName);

            // Assert
            Assert.That(result, Is.EqualTo(false));
        }        
    }
}