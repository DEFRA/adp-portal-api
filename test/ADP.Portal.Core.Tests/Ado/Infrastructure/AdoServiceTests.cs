using ADP.Portal.Core.Ado.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints;
using Moq;
using AutoFixture;
using NUnit.Framework;


namespace ADP.Portal.Core.Tests.Ado.Infrastructure
{
    [TestFixture]
    public class AdoServiceTests
    {
        private readonly AdoService adoService;
        private readonly Mock<ILogger<AdoService>> loggerMock;
        private readonly Mock<IVssConnection> vssConnectionMock;

        public AdoServiceTests()
        {
            loggerMock = new Mock<ILogger<AdoService>>();
            vssConnectionMock = new Mock<IVssConnection>();
            adoService = new AdoService(loggerMock.Object, Task.FromResult(vssConnectionMock.Object));
        }

        [Test]
        public async Task GetTeamProjectAsync_ReturnsTeamProject()
        {
            var expectedProject = new TeamProject { Name = "TestProject" };
            var mockProjectClient = new Mock<ProjectHttpClient>(new Uri("https://mock"), new VssCredentials());
            mockProjectClient.Setup(client => client.GetProject(It.IsAny<string>(), null, false, null)).ReturnsAsync(expectedProject);
            vssConnectionMock.Setup(conn => conn.GetClientAsync<ProjectHttpClient>(It.IsAny<CancellationToken>())).ReturnsAsync(mockProjectClient.Object);

            var result = await adoService.GetTeamProjectAsync("TestProject");

            Assert.That(result, Is.Not.Null);
            Assert.That(expectedProject.Name, Is.EqualTo(result.Name));
        }

        [Test]
        public void GetTeamProjectAsync_WithNonexistentProject_ReturnsNull()
        {
            var mockProjectClient = new Mock<ProjectHttpClient>(new Uri("https://mock"), new VssCredentials());
            mockProjectClient.Setup(client => client.GetProject(It.IsAny<string>(), null, false, null))
                .ThrowsAsync(new ProjectDoesNotExistWithNameException());

            vssConnectionMock.Setup(conn => conn.GetClientAsync<ProjectHttpClient>(It.IsAny<CancellationToken>())).ReturnsAsync(mockProjectClient.Object);

            Assert.ThrowsAsync<ProjectDoesNotExistWithNameException>(async () => await adoService.GetTeamProjectAsync("NonexistentProject"));
        }

        [Test]
        public async Task ShareServiceEndpointsAsync_CallsShareServiceEndpointAsync()
        {
            // Arrange
            var mockServiceEndpointClient = new Mock<ServiceEndpointHttpClient>(new Uri("https://mock"), new VssCredentials());
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

            
            mockServiceEndpointClient.Setup(client => client.GetServiceEndpointsAsync(It.IsAny<string>(), null, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceEndpoint);

            mockServiceEndpointClient.Setup(client => client.ShareServiceEndpointAsync(It.IsAny<Guid>(), It.IsAny<List<ServiceEndpointProjectReference>>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            vssConnectionMock.Setup(conn => conn.GetClientAsync<ServiceEndpointHttpClient>(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockServiceEndpointClient.Object);


            // Act
            await adoService.ShareServiceEndpointsAsync("TestProject", new List<string> { "TestServiceEndpoint" }, new TeamProjectReference { Id = Guid.NewGuid() });

            // Assert
            mockServiceEndpointClient.Verify(client => client.ShareServiceEndpointAsync(It.IsAny<Guid>(), It.IsAny<List<ServiceEndpointProjectReference>>(), null, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
