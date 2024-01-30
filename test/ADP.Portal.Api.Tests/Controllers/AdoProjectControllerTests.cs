using ADP.Portal.Api.Config;
using ADP.Portal.Api.Controllers;
using ADP.Portal.Api.Models;
using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Services;
using AutoFixture;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Core.WebApi;
using Moq;
using NUnit.Framework;
using System.Reflection;

namespace ADP.Portal.Api.Tests.Controllers
{
    [TestFixture]
    public class AdoProjectControllerTests
    {
        private readonly AdoProjectController controller;
        private readonly Mock<ILogger<AdoProjectController>> loggerMock;
        private readonly Mock<IOptions<AdpAdoProjectConfig>> configMock;
        private readonly Mock<IAdoProjectService> serviceMock;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        }

        public AdoProjectControllerTests()
        {
            loggerMock = new Mock<ILogger<AdoProjectController>>();
            configMock = new Mock<IOptions<AdpAdoProjectConfig>>();
            serviceMock = new Mock<IAdoProjectService>();
            controller = new AdoProjectController(loggerMock.Object, configMock.Object, serviceMock.Object);
        }

        [Test]
        public async Task GetAdoProject_ReturnsNotFound_WhenProjectDoesNotExist()
        {
            // Arrange
            string projectName = "test";
            TeamProjectReference? project = null;
            serviceMock.Setup(s => s.GetProjectAsync(projectName)).ReturnsAsync(project);

            // Act
            var result = await controller.GetAdoProject(projectName);
            var notFoundResults = result as NotFoundResult;

            // Assert
            Assert.That(notFoundResults, Is.Not.Null);
            if (notFoundResults != null)
            {
                Assert.That(notFoundResults.StatusCode, Is.EqualTo(404));
            }
            
        }

        [Test]
        public async Task GetAdoProject_ReturnsOk_WhenProjectExists()
        {
            // Arrange
            string projectName = "test";
            var fixture = new Fixture();
            var project = fixture.Build<TeamProjectReference>().Create();
            serviceMock.Setup(s => s.GetProjectAsync(projectName)).ReturnsAsync(project);

            // Act
            var result = await controller.GetAdoProject(projectName);
            var okFoundResults = result as OkObjectResult;

            // Assert
            Assert.That(okFoundResults, Is.Not.Null);
            if(okFoundResults != null)
            {
                Assert.That(okFoundResults.StatusCode, Is.EqualTo(200));
            }
        }

        [Test]
        public async Task OnBoardAsync_ReturnsNotFound_WhenProjectDoesNotExist()
        {
            // Arrange
            string projectName = "test";
            var fixture = new Fixture();
            var onBoardRequest = fixture.Build<OnBoardAdoProjectRequest>().Create();
            TeamProjectReference? project = null;
            serviceMock.Setup(s => s.GetProjectAsync(projectName)).ReturnsAsync(project);

            // Act
            var result = await controller.OnBoardAsync(projectName, onBoardRequest);
            var notFoundResults = result as NotFoundResult;

            // Assert
            Assert.That(notFoundResults, Is.Not.Null);
            if (notFoundResults != null)
            {
                Assert.That(notFoundResults.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public async Task OnBoardAsync_Update_WhenProjectExist()
        {
            // Arrange
            string projectName = "test";
            var fixture = new Fixture();
            var onBoardRequest = fixture.Build<OnBoardAdoProjectRequest>().Create();
            var project = fixture.Build<TeamProjectReference>().Create();
            var adpAdoProjectConfig = fixture.Build<AdpAdoProjectConfig>().Create();
            serviceMock.Setup(s => s.GetProjectAsync(projectName)).ReturnsAsync(project);
            serviceMock.Setup(s => s.OnBoardAsync(projectName, It.IsAny<AdoProject>())).Returns(Task.CompletedTask);
            configMock.Setup(c => c.Value).Returns(adpAdoProjectConfig);

            // Act
            var result = await controller.OnBoardAsync(projectName, onBoardRequest);
            var noContentResult = result as NoContentResult;

            // Assert
            Assert.That(noContentResult, Is.Not.Null);
            serviceMock.Verify(client => client.OnBoardAsync(It.IsAny<string>(), It.IsAny<AdoProject>()), Times.Once);
            if (noContentResult != null)
            {
                Assert.That(noContentResult.StatusCode, Is.EqualTo(204));
            }
        }
    }
}
