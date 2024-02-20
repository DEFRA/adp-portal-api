using ADP.Portal.Api.Controllers;
using ADP.Portal.Api.Services;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;
using System.Reflection;

namespace ADP.Portal.Api.Tests.Controllers
{
    [TestFixture]
    public class OpenVpnUserControllerTests
    {
        private readonly OpenVpnUserController controller;
        private readonly IUserService serviceMock;


        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        }

        public OpenVpnUserControllerTests()
        {
            serviceMock = Substitute.For<IUserService>();
            controller = new OpenVpnUserController(serviceMock);
        }        

        [Test]
        public async Task OnAddOpenVPNUser_ReturnsNotAdded_WhenUserDoesNotExist()
        {
            // Arrange
            string userPrincipalName = "test";        
            await serviceMock.AddOpenVPNUser(userPrincipalName);

            // Act
            var result = await controller.AddOpenVPNUser(userPrincipalName);
            var notFoundResults = result as NotFoundResult;

            // Assert
            Assert.That(notFoundResults, Is.Not.Null);
            if (notFoundResults != null)
            {
                Assert.That(notFoundResults.StatusCode, Is.EqualTo(404));
            }
        }
        
        [Test]
        public async Task OnAddOpenVPNUser_Add_WhenUserExist()
        {
            // Arrange
            string userPrincipalName = "test";
            await serviceMock.AddOpenVPNUser(userPrincipalName);

            // Act
            var result = await controller.AddOpenVPNUser(userPrincipalName);
            var noContentResult = result as NoContentResult;            

            // Assert
            Assert.That(noContentResult, Is.Not.Null);
            await serviceMock.Received().AddOpenVPNUser(Arg.Any<string>());
            if (noContentResult != null)
            {
                Assert.That(noContentResult.StatusCode, Is.EqualTo(204));
            }
        }
    }
}
