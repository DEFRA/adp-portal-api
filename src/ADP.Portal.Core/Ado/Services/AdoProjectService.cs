using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using Microsoft.Extensions.Logging;
using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Infrastructure;

namespace ADP.Portal.Core.Ado.Services
{
    public class AdoProjectService : IAdoProjectService
    {
        private readonly VssConnection _vssConnection;
        private readonly ILogger<AdoProjectService> _logger;
        private readonly ServiceEndpointHelper _serviceEndpointHelper;

        public AdoProjectService(Task<VssConnection> vssConnection, ILogger<AdoProjectService> logger)
        {
            _vssConnection = vssConnection.Result;
            _logger = logger;
            _serviceEndpointHelper = new ServiceEndpointHelper(logger);
        }

        public async Task<TeamProjectReference?> GetProjectAsync(string projectName)
        {
            try
            {
                _logger.LogInformation($"Getting project {projectName}");
                using var projectClient = await _vssConnection.GetClientAsync<ProjectHttpClient>();

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
            await AddServiceEndpointsAsync(onBoardProject, adpProject);
        }

        private async Task AddServiceEndpointsAsync(TeamProjectReference onBoardProject, AdoProject adpProject)
        {
            using var serviceEndpointClient = await _vssConnection.GetClientAsync<ServiceEndpointHttpClient>();

            _logger.LogInformation($"Getting service endpoints for project {adpProject.Name}");

            var endpoints = await serviceEndpointClient.GetServiceEndpointsAsync(adpProject.Name);

            foreach (var serviceConnection in adpProject.ServiceConnections)
            {
                await _serviceEndpointHelper.ShareServiceEndpointAsync(serviceConnection, onBoardProject, endpoints, serviceEndpointClient);
            }
        }
    }
}
