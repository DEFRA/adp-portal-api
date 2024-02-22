using ADP.Portal.Core.Ado.Infrastructure;
using ADP.Portal.Core.Ado.Services;
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

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        }

        public AzureAADGroupServiceTests()
        {
            serviceMock = Substitute.For<IUserGroupService>();
        }

        [Test]
        public void Constructor_WithValidParameters_SetsAzureAADGroupService()
        {
            // Arrange
            var logger = Substitute.For<ILogger<AzureAADGroupService>>();
            string? userPrincipalName = "testuser";

            // Act
            //var result = AddToAADGroupAsync(Guid groupId, userPrincipalName);

            // Assert
            //Assert.That(projectService, Is.Not.Null);
        }
    }
}
