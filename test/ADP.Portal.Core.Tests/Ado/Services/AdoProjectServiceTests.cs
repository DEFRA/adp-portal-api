using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Infrastructure;
using ADP.Portal.Core.Ado.Services;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Moq;

namespace ADP.Portal.Core.Tests.Ado.Services
{
    public class AdoProjectServiceTests
    {
        private readonly Mock<IAdoService> adoServiceMock;
        private readonly Mock<ILogger<AdoProjectService>> loggerMock;
        private readonly AdoProjectService adoProjectService;

        public AdoProjectServiceTests()
        {
            adoServiceMock = new Mock<IAdoService>();
            loggerMock = new Mock<ILogger<AdoProjectService>>();
            adoProjectService = new AdoProjectService(adoServiceMock.Object, loggerMock.Object);
        }

        [Fact]
        public async Task GetProjectAsync_ReturnsProject_WhenProjectExists()
        {
            // Arrange
            var projectName = "TestProject";
            var project = new TeamProject();
            adoServiceMock.Setup(x => x.GetTeamProjectAsync(projectName)).ReturnsAsync(project);

            // Act
            var result = await adoProjectService.GetProjectAsync(projectName);

            // Assert
            Assert.Equal(project, result);
        }

        [Fact]
        public async Task GetProjectAsync_ReturnsNull_WhenProjectDoesNotExist()
        {
            // Arrange
            var projectName = "TestProject";
            adoServiceMock.Setup(x => x.GetTeamProjectAsync(projectName)).Throws<ProjectDoesNotExistWithNameException>();

            // Act
            var result = await adoProjectService.GetProjectAsync(projectName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task OnBoardAsync_CallsAdoServiceMethods()
        {
            // Arrange
            var adpProjectName = "TestProject";
            var onboardProject = new AdoProject();

            // Act
            await adoProjectService.OnBoardAsync(adpProjectName, onboardProject);

            // Assert
            adoServiceMock.Verify(x => x.ShareServiceEndpointsAsync(adpProjectName, onboardProject.ServiceConnections, onboardProject.ProjectReference), Times.Once);
            adoServiceMock.Verify(x => x.AddEnvironmentsAsync(onboardProject.Environments, onboardProject.ProjectReference), Times.Once);
            adoServiceMock.Verify(x => x.ShareAgentPoolsAsync(adpProjectName, onboardProject.AgentPools, onboardProject.ProjectReference), Times.Once);
            if (onboardProject.VariableGroups != null)
            {
                adoServiceMock.Verify(x => x.AddOrUpdateVariableGroupsAsync(onboardProject.VariableGroups, onboardProject.ProjectReference), Times.Once);
            }
        }
    }
}
