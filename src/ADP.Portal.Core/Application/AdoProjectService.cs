using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints;
using Microsoft.Extensions.Logging;
using ADP.Portal.Core.Domain;

namespace ADP.Portal.Core.Application
{
    public class AdoProjectService : IAdoProjectService
    {
        private readonly VssConnection _vssConnection;
        private ILogger<AdoProjectService> _logger { get; }

        public AdoProjectService(Task<VssConnection> vssConnection, ILogger<AdoProjectService> logger)
        {
            _vssConnection = vssConnection.Result;
            _logger = logger;
        }


        public async Task<TeamProjectReference?> GetProjectAsync(string projectName)
        {
            try
            {
                _logger.LogInformation($"Getting project {projectName}");
                var projectClient = await _vssConnection.GetClientAsync<ProjectHttpClient>();

                var project = await projectClient.GetProject(projectName);
                return project;
            }
            catch (ProjectDoesNotExistWithNameException)
            {
                return null;
            }
        }

        public async Task OnBoardAsync(TeamProjectReference onBoardProject, AdoProject adpProject)
        {
            var serviceEndpointClient = await _vssConnection.GetClientAsync<ServiceEndpointHttpClient>();

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
    }
}
