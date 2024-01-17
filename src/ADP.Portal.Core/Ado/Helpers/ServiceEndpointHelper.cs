using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints;

namespace ADP.Portal.Core.Ado.Infrastructure
{
    public class ServiceEndpointHelper
    {
        private readonly ILogger _logger;

        public ServiceEndpointHelper(ILogger logger)
        {
            _logger = logger;
        }

        public async Task ShareServiceEndpointAsync(string serviceConnection,
        TeamProjectReference onBoardProject,
        List<ServiceEndpoint> endpoints,
        ServiceEndpointHttpClient serviceEndpointClient)
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
