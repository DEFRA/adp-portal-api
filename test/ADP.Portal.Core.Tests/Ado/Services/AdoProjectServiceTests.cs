using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Infrastructure;
using ADP.Portal.Core.Ado.Services;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Moq;
using NUnit.Framework;

namespace ADP.Portal.Core.Tests.Ado.Services
{
    [TestFixture]
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

        [Test]
        public async Task GetProjectAsync_ReturnsProject_WhenProjectExists()
        {
            var projectName = "TestProject";
            var project = new TeamProject();
            adoServiceMock.Setup(x => x.GetTeamProjectAsync(projectName)).ReturnsAsync(project);

            var result = await adoProjectService.GetProjectAsync(projectName);

            Assert.That(result,Is.EqualTo(project));

        }

        [Test]
        public async Task GetProjectAsync_ReturnsNull_WhenProjectDoesNotExist()
        {

            var projectName = "TestProject";
            adoServiceMock.Setup(x => x.GetTeamProjectAsync(projectName)).Throws<ProjectDoesNotExistWithNameException>();

            var result = await adoProjectService.GetProjectAsync(projectName);

            Assert.That(result, Is.Null);

        }

        [Test]
        public async Task OnBoardAsync_CallsAdoServiceMethods()
        {
            var adpProjectName = "TestProject";
            var onboardProject = new AdoProject(It.IsAny<TeamProjectReference>(),
                It.IsAny<List<string>>(), It.IsAny<List<string>>(), It.IsAny<List<AdoEnvironment>>() , It.IsAny<List<AdoVariableGroup>?>()
                );

            await adoProjectService.OnBoardAsync(adpProjectName, onboardProject);

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
