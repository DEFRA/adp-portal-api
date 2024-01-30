using ADP.Portal.Core.Ado.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints;
using Moq;
using AutoFixture;
using NUnit.Framework;
using ADP.Portal.Core.Ado.Entities;
using ProjectReference = Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi.ProjectReference;
using DistributedTask = Microsoft.TeamFoundation.DistributedTask.WebApi;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Mapster;




namespace ADP.Portal.Core.Tests.Ado.Infrastructure
{
    [TestFixture]
    public class AdoServiceTests
    {
        private readonly Mock<IVssConnection> vssConnectionMock;
        private readonly Mock<ServiceEndpointHttpClient> serviceEndpointClientMock;
        private readonly Mock<DistributedTask.TaskAgentHttpClient> taskAgentClientMock;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig<AdoVariableGroup, DistributedTask.VariableGroupParameters>.NewConfig()
                .Map(dest => dest.VariableGroupProjectReferences, src => new List<DistributedTask.VariableGroupProjectReference>() { new() { Name = src.Name, Description = src.Description } })
                .Map(dest => dest.Variables, src => src.Variables.ToDictionary(v => v.Name, v => new DistributedTask.VariableValue(v.Value, v.IsSecret)));
        }

        public AdoServiceTests()
        {
            vssConnectionMock = new Mock<IVssConnection>();
            serviceEndpointClientMock = new Mock<ServiceEndpointHttpClient>(new Uri("https://mock"), new VssCredentials());
            taskAgentClientMock = new Mock<DistributedTask.TaskAgentHttpClient>(new Uri("https://mock"), new VssCredentials());
        }

        [Test]
        public void Constructor_WithValidParameters_SetsAdoService()
        {
            var loggerMock = new Mock<ILogger<AdoService>>();
            var logger = loggerMock.Object;

            var projectService = new AdoService(logger, Task.FromResult(vssConnectionMock.Object));

            Assert.That(projectService, Is.Not.Null);
        }

        [Test]
        public async Task GetTeamProjectAsync_ReturnsTeamProject()
        {
            // Arrange
            var expectedProject = new TeamProject { Name = "TestProject" };
            var mockProjectClient = new Mock<ProjectHttpClient>(new Uri("https://mock"), new VssCredentials());
            mockProjectClient.Setup(client => client.GetProject(It.IsAny<string>(), null, false, null)).ReturnsAsync(expectedProject);
            vssConnectionMock.Setup(conn => conn.GetClientAsync<ProjectHttpClient>(It.IsAny<CancellationToken>())).ReturnsAsync(mockProjectClient.Object);
            var loggerMock = new Mock<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock.Object, Task.FromResult(vssConnectionMock.Object));


            var result = await adoService.GetTeamProjectAsync("TestProject");

            Assert.That(result, Is.Not.Null);
            Assert.That(expectedProject.Name, Is.EqualTo(result.Name));
        }

        [Test]
        public void GetTeamProjectAsync_WithNonexistentProject_ReturnsNull()
        {
            // Arrange
            var mockProjectClient = new Mock<ProjectHttpClient>(new Uri("https://mock"), new VssCredentials());
            mockProjectClient.Setup(client => client.GetProject(It.IsAny<string>(), null, false, null))
                .ThrowsAsync(new ProjectDoesNotExistWithNameException());
            var loggerMock = new Mock<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock.Object, Task.FromResult(vssConnectionMock.Object));

            vssConnectionMock.Setup(conn => conn.GetClientAsync<ProjectHttpClient>(It.IsAny<CancellationToken>())).ReturnsAsync(mockProjectClient.Object);

            Assert.ThrowsAsync<ProjectDoesNotExistWithNameException>(async () => await adoService.GetTeamProjectAsync("NonexistentProject"));
        }

        [Test]
        public async Task ShareServiceEndpointsAsync_CallsShareServiceEndpointAsync()
        {
            // Arrange

            var fixture = new Fixture();
            var serviceEndpointProjectReferences = fixture.Build<ServiceEndpointProjectReference>()
                .With(reference => reference.Name, "TestProject")
                .With(reference => reference.ProjectReference, new ProjectReference { Id = Guid.NewGuid() })
                .OmitAutoProperties()
                .CreateMany(1).ToList();
            var serviceEndpoint = fixture.Build<ServiceEndpoint>()
                .With(endpoint => endpoint.Id, Guid.NewGuid())
                .With(endpoint => endpoint.Name, "TestServiceEndpoint")
                .With(endpoint => endpoint.Type, "git")
                .With(endpoint => endpoint.Url, new Uri("https://example.com"))
                .With(endpoint => endpoint.ServiceEndpointProjectReferences, serviceEndpointProjectReferences)
                .OmitAutoProperties()
                .CreateMany(1).ToList();


            serviceEndpointClientMock.Setup(client => client.GetServiceEndpointsAsync(It.IsAny<string>(), null, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceEndpoint);

            serviceEndpointClientMock.Setup(client => client.ShareServiceEndpointAsync(It.IsAny<Guid>(), It.IsAny<List<ServiceEndpointProjectReference>>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            vssConnectionMock.Setup(conn => conn.GetClientAsync<ServiceEndpointHttpClient>(It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceEndpointClientMock.Object);

            var loggerMock = new Mock<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock.Object, Task.FromResult(vssConnectionMock.Object));


            // Act
            await adoService.ShareServiceEndpointsAsync("TestProject", new List<string> { "TestServiceEndpoint" }, new TeamProjectReference { Id = Guid.NewGuid() });

            // Assert
            serviceEndpointClientMock.Verify(client => client.ShareServiceEndpointAsync(It.IsAny<Guid>(), It.IsAny<List<ServiceEndpointProjectReference>>(), null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ShareServiceEndpointsAsync_LogsInformationMessage_WhenIsAlreadySharedIsTrue()
        {
            // Arrange

            var adpProjectName = "TestProject";
            var serviceConnections = new List<string> { "TestServiceConnection" };
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid() };
            var serviceEndpoint = new ServiceEndpoint
            {
                Name = "TestServiceConnection",
                ServiceEndpointProjectReferences = new List<ServiceEndpointProjectReference>
                {
                    new() { ProjectReference = new ProjectReference { Id = onBoardProject.Id } }
                }
            };

            serviceEndpointClientMock.Setup(x => x.GetServiceEndpointsAsync(adpProjectName, null, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ServiceEndpoint> { serviceEndpoint });

            vssConnectionMock.Setup(conn => conn.GetClientAsync<ServiceEndpointHttpClient>(It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceEndpointClientMock.Object);

            var loggerMock = new Mock<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock.Object, Task.FromResult(vssConnectionMock.Object));

            // Act
            await adoService.ShareServiceEndpointsAsync(adpProjectName, serviceConnections, onBoardProject);

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("already shared")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Test]
        public async Task ShareServiceEndpointsAsync_LogsWarningMessage_WhenEndpointIsNull()
        {
            // Arrange
            var adpProjectName = "TestProject";
            var serviceConnections = new List<string> { "NonExistentServiceConnection" };
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid() };
            var serviceEndpoint = new ServiceEndpoint
            {
                Name = "TestServiceConnection",
                ServiceEndpointProjectReferences = new List<ServiceEndpointProjectReference>
                {
                    new() { ProjectReference = new ProjectReference { Id = onBoardProject.Id } }
                }
            };
            serviceEndpointClientMock.Setup(x => x.GetServiceEndpointsAsync(adpProjectName, null, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ServiceEndpoint> { serviceEndpoint });

            vssConnectionMock.Setup(conn => conn.GetClientAsync<ServiceEndpointHttpClient>(It.IsAny<CancellationToken>()))
               .ReturnsAsync(serviceEndpointClientMock.Object);

            var loggerMock = new Mock<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock.Object, Task.FromResult(vssConnectionMock.Object));

            // Act
            await adoService.ShareServiceEndpointsAsync(adpProjectName, serviceConnections, onBoardProject);

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Warning),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("not found")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Test]
        public async Task AddEnvironmentsAsync_LogsInformationMessage_WhenEnvironmentExists()
        {
            // Arrange
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var adoEnvironments = new List<AdoEnvironment> { new("TestEnvironment", "") };

            var environments = new List<DistributedTask.EnvironmentInstance> { new DistributedTask.EnvironmentInstance { Name = "TestEnvironment" } };

            taskAgentClientMock.Setup(x => x.GetEnvironmentsAsync(onBoardProject.Id, null, null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(environments);

            vssConnectionMock.Setup(conn => conn.GetClientAsync<DistributedTask.TaskAgentHttpClient>(It.IsAny<CancellationToken>()))
               .ReturnsAsync(taskAgentClientMock.Object);

            var loggerMock = new Mock<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock.Object, Task.FromResult(vssConnectionMock.Object));

            // Act
            await adoService.AddEnvironmentsAsync(adoEnvironments, onBoardProject);

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("already exists")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Test]
        public async Task AddEnvironmentsAsync_CreatesEnvironment_WhenEnvironmentDoesNotExist()
        {
            // Arrange
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var adoEnvironments = new List<AdoEnvironment> { new("TestEnvironment", "") };
            var environments = new List<DistributedTask.EnvironmentInstance>();
            taskAgentClientMock.Setup(x => x.GetEnvironmentsAsync(onBoardProject.Id, null, null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(environments);
            vssConnectionMock.Setup(conn => conn.GetClientAsync<DistributedTask.TaskAgentHttpClient>(It.IsAny<CancellationToken>()))
             .ReturnsAsync(taskAgentClientMock.Object);
            var loggerMock = new Mock<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock.Object, Task.FromResult(vssConnectionMock.Object));

            // Act
            await adoService.AddEnvironmentsAsync(adoEnvironments, onBoardProject);

            // Assert
            taskAgentClientMock.Verify(x => x.AddEnvironmentAsync(onBoardProject.Id, It.IsAny<DistributedTask.EnvironmentCreateParameter>(), null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ShareAgentPoolsAsync_LogsInformationMessage_WhenAgentPoolExists()
        {
            // Arrange
            var adpProjectName = "TestProject";
            var adoAgentPoolsToShare = new List<string> { "TestAgentPool" };
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var adpAgentQueues = new List<DistributedTask.TaskAgentQueue> { new() { Name = "TestAgentPool" } };
            var agentPools = new List<DistributedTask.TaskAgentQueue> { new() { Name = "TestAgentPool" } };
            taskAgentClientMock.Setup(x => x.GetAgentQueuesAsync(adpProjectName, string.Empty, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(adpAgentQueues);
            taskAgentClientMock.Setup(x => x.GetAgentQueuesAsync(onBoardProject.Id, null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(agentPools);
            vssConnectionMock.Setup(conn => conn.GetClientAsync<DistributedTask.TaskAgentHttpClient>(It.IsAny<CancellationToken>()))
                .ReturnsAsync(taskAgentClientMock.Object);

            var loggerMock = new Mock<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock.Object, Task.FromResult(vssConnectionMock.Object));

            // Act
            await adoService.ShareAgentPoolsAsync(adpProjectName, adoAgentPoolsToShare, onBoardProject);

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("already exists")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Test]
        public async Task ShareAgentPoolsAsync_CreatesAgentPool_WhenAgentPoolDoesNotExist()
        {
            // Arrange
            var adpProjectName = "TestProject";
            var adoAgentPoolsToShare = new List<string> { "TestAgentPool" };
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var adpAgentQueues = new List<DistributedTask.TaskAgentQueue> { new() { Name = "TestAgentPool" } };
            var agentPools = new List<DistributedTask.TaskAgentQueue>();
            taskAgentClientMock.Setup(x => x.GetAgentQueuesAsync(adpProjectName, string.Empty, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(adpAgentQueues);
            taskAgentClientMock.Setup(x => x.GetAgentQueuesAsync(onBoardProject.Id, null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(agentPools);
            vssConnectionMock.Setup(conn => conn.GetClientAsync<DistributedTask.TaskAgentHttpClient>(It.IsAny<CancellationToken>()))
                .ReturnsAsync(taskAgentClientMock.Object);
            var loggerMock = new Mock<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock.Object, Task.FromResult(vssConnectionMock.Object));

            // Act
            await adoService.ShareAgentPoolsAsync(adpProjectName, adoAgentPoolsToShare, onBoardProject);

            // Assert
            taskAgentClientMock.Verify(x => x.AddAgentQueueAsync(onBoardProject.Id, It.IsAny<DistributedTask.TaskAgentQueue>(), null, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task AddOrUpdateVariableGroupsAsync_CreatesVariableGroup_WhenVariableGroupDoesNotExist()
        {
            // Arrange
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var fixture = new Fixture();
            
            var adoVariables = fixture.Build<AdoVariable>().CreateMany(2).ToList();
            var adoVariableGroup = new AdoVariableGroup("TestVariableGroup", adoVariables, "TestVariableGroup Description");
            var adoVariableGroups = new List<AdoVariableGroup> { adoVariableGroup };
            var variableGroups = new List<DistributedTask.VariableGroup>();
            taskAgentClientMock.Setup(x => x.GetVariableGroupsAsync(onBoardProject.Id,null,null,null,null,null,null, It.IsAny<CancellationToken>())).ReturnsAsync(variableGroups);
            vssConnectionMock.Setup(conn => conn.GetClientAsync<DistributedTask.TaskAgentHttpClient>(It.IsAny<CancellationToken>()))
                .ReturnsAsync(taskAgentClientMock.Object);

            var loggerMock = new Mock<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock.Object, Task.FromResult(vssConnectionMock.Object));

            // Act
            await adoService.AddOrUpdateVariableGroupsAsync(adoVariableGroups, onBoardProject);

            // Assert
            taskAgentClientMock.Verify(x => x.AddVariableGroupAsync(It.IsAny<DistributedTask.VariableGroupParameters>(),null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task AddOrUpdateVariableGroupsAsync_UpdatesVariableGroup_WhenVariableGroupExists()
        {
            // Arrange
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var fixture = new Fixture();
            var adoVariables = fixture.Build<AdoVariable>().CreateMany(2).ToList();
            var adoVariableGroup = new AdoVariableGroup("TestVariableGroup", adoVariables, "TestVariableGroup Description");
            var adoVariableGroups = new List<AdoVariableGroup> { adoVariableGroup };
            var variableGroups = new List<DistributedTask.VariableGroup> { new DistributedTask.VariableGroup { Name = "TestVariableGroup", Id = 1 } };
            taskAgentClientMock.Setup(x => x.GetVariableGroupsAsync(onBoardProject.Id, null, null, null, null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(variableGroups);
            vssConnectionMock.Setup(conn => conn.GetClientAsync<DistributedTask.TaskAgentHttpClient>(It.IsAny<CancellationToken>()))
                .ReturnsAsync(taskAgentClientMock.Object);

            var loggerMock = new Mock<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock.Object, Task.FromResult(vssConnectionMock.Object));

            // Act
            await adoService.AddOrUpdateVariableGroupsAsync(adoVariableGroups, onBoardProject);

            // Assert
            taskAgentClientMock.Verify(x => x.UpdateVariableGroupAsync(It.IsAny<int>(), It.IsAny<DistributedTask.VariableGroupParameters>(),null, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
