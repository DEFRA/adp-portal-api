using ADP.Portal.Api.Config;
using ADP.Portal.Api.Controllers;
using ADP.Portal.Core.Ado.Services;
using ADP.Portal.Core.Azure.Services;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Core.WebApi;
using NSubstitute;
using NUnit.Framework;
using System.Reflection;

namespace ADP.Portal.Api.Tests.Controllers
{
    [TestFixture]
    public class UserAADGroupControllerTests
    {
        private readonly UserAADGroupController controller;
        private readonly ILogger<UserAADGroupController> loggerMock;
        private readonly IOptions<AADGroupConfig> configMock;
        private readonly IUserGroupService serviceMock;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        }

        public UserAADGroupControllerTests(IUserGroupService userGroupService, IOptions<AADGroupConfig> aadGroupConfig)
        {
            loggerMock = Substitute.For<ILogger<UserAADGroupController>> ();
            configMock = Substitute.For<IOptions<AADGroupConfig>>();
            serviceMock = Substitute.For<IUserGroupService>();
            controller = new UserAADGroupController(loggerMock, serviceMock, configMock);
        }

        public async Task GetAdoProject_ReturnsNotFound_WhenProjectDoesNotExist()
        {
            // Arrange
            string userPrincipalName = "testUser";
          
            // Act
            var result = await controller.AddUserToOpenVpnGroup(userPrincipalName);
            var notFoundResults = result as NotFoundResult;

            // Assert
            Assert.That(notFoundResults, Is.Not.Null);
            if (notFoundResults != null)
            {
                Assert.That(notFoundResults.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public async Task OnAddADUser_ReturnsNotAdded_WhenUserDoesNotExist()
        {
            // Arrange
            string userPrincipalName = "testUser";

            // Act
            var result = await controller.AddUserToOpenVpnGroup(userPrincipalName);
            var notFoundResults = result as NotFoundResult;

            // Assert
            Assert.That(notFoundResults, Is.Null);
            if (notFoundResults != null)
            {
                Assert.That(notFoundResults.StatusCode, Is.EqualTo(404));
            }
        }
        
        [Test]
        public async Task OnAddADUser_Add_WhenUserExist()
        {
            // Arrange
            string userPrincipalName = "testUser";

            // Act
            var result = await controller.AddUserToOpenVpnGroup(userPrincipalName);
            var notFoundResults = result as NotFoundResult;

            // Assert
            Assert.That(notFoundResults, Is.Not.Null);
            if (notFoundResults != null)
            {
                Assert.That(notFoundResults.StatusCode, Is.EqualTo(404));
            }
        }
    }
}
