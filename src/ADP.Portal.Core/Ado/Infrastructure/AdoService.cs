using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints;
using Microsoft.VisualStudio.Services.WebApi;
using ADP.Portal.Core.Ado.Entities;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using ProjectReference = Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi.ProjectReference;

namespace ADP.Portal.Core.Ado.Infrastructure
{
    public class AdoService : IAdoService
    {
        private readonly ILogger _logger;
        private readonly VssConnection _vssConnection;

        public AdoService(ILogger<AdoService> logger, Task<VssConnection> vssConnection)
        {
            _logger = logger;
            _vssConnection = vssConnection.Result;
        }

        public async Task ShareServiceEndpointsAsync(AdoProject adpProject, TeamProjectReference onBoardProject)
        {
            using var serviceEndpointClient = await _vssConnection.GetClientAsync<ServiceEndpointHttpClient>();

            _logger.LogInformation($"Getting service endpoints for project {adpProject.Name}");

            var endpoints = await serviceEndpointClient.GetServiceEndpointsAsync(adpProject.Name);

            foreach (var serviceConnection in adpProject.ServiceConnections)
            {
                var endpoint = endpoints.FirstOrDefault(e => e.Name.Equals(serviceConnection, StringComparison.OrdinalIgnoreCase));

                if (endpoint != null)
                {
                    var isAlreadyShared = endpoint.ServiceEndpointProjectReferences.Any(r => r.ProjectReference.Id == onBoardProject.Id);
                    if (!isAlreadyShared)
                    {
                        _logger.LogInformation($"Sharing service endpoint {serviceConnection} with project {onBoardProject.Name}");

                        var serviceEndpointProjectReferences = new List<ServiceEndpointProjectReference>();
                        var projectReference = new ServiceEndpointProjectReference
                        {
                            Name = serviceConnection,
                            ProjectReference = new ProjectReference
                            {
                                Name = onBoardProject.Name,
                                Id = onBoardProject.Id
                            }
                        };
                        serviceEndpointProjectReferences.Add(projectReference);
                        await serviceEndpointClient.ShareServiceEndpointAsync(endpoint.Id, serviceEndpointProjectReferences);
                    }
                    else
                    {
                        _logger.LogInformation($"Service endpoint {serviceConnection} already shared with project {onBoardProject.Name}");
                    }
                }
                else
                {
                    _logger.LogWarning($"Service endpoint {serviceConnection} not found");
                }
            }
        }

        public async Task AddEnvironments(List<AdoEnvironment> adoEnvironments, TeamProjectReference onBoardProject)
        {
            using var environmentClient = await _vssConnection.GetClientAsync<TaskAgentHttpClient>();

            _logger.LogInformation($"Getting environments for project {onBoardProject.Name}");

            var environments = await environmentClient.GetEnvironmentsAsync(onBoardProject.Id);

            foreach (var environment in adoEnvironments)
            {

                var IsEnvironmentExists = environments.Any(e => e.Name.Equals(environment.Name, StringComparison.OrdinalIgnoreCase));

                if (IsEnvironmentExists)
                {
                    _logger.LogInformation($"Environment {environment.Name} already exists");
                    return;
                }

                _logger.LogInformation($"Creating environment {environment.Name}");

                var environmentParameter = new EnvironmentCreateParameter()
                {
                    Name = environment.Name
                };

                await environmentClient.AddEnvironmentAsync(onBoardProject.Id, environmentParameter);

                _logger.LogInformation($"Environment {environment.Name} created");
            }
        }

        public async Task<TeamProject> GetTeamProjectAsync(string projectName)
        {
            _logger.LogInformation($"Getting project {projectName}");
            using var projectClient = await _vssConnection.GetClientAsync<ProjectHttpClient>();

            var project = await projectClient.GetProject(projectName);
            return project;
        }
    }
}
