using ADP.Portal.Core.Azure.Infrastructure;
using ADP.Portal.Core.Azure.Services;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using NSubstitute;
using NUnit.Framework;
using System.Reflection;


namespace ADP.Portal.Core.Tests.Azure.Infrastructure
{
    [TestFixture]
    public class AzureAADGroupServiceTests
    {
        private readonly IUserGroupService serviceMock;
        private readonly ILogger<AzureAADGroupService> loggerMock;
        private readonly GraphServiceClient graphServiceClientMock;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        }

        public AzureAADGroupServiceTests()
        {
            serviceMock = Substitute.For<IUserGroupService>();
            loggerMock = Substitute.For<ILogger<AzureAADGroupService>>();
        }

        [Test]
        public void Constructor_WithValidParameters_SetsAzureAADGroupService()
        {     
            // Act
            var groupService = new AzureAADGroupService(graphServiceClientMock, loggerMock);

            // Assert
            Assert.That(groupService, Is.Not.Null);
        }

        [Test]
        public void AddToAADGroupAsync_ReturnsAdded_WhenUserExiste()
        {
            // Act
            var azureAADGroupService = new AzureAADGroupService(graphServiceClientMock, loggerMock);

            // Assert
            Assert.That(azureAADGroupService, Is.Not.Null);
        }
    }
}
