using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints;
using Microsoft.VisualStudio.Services.WebApi;
using ADP.Portal.Core.Ado.Entities;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using ProjectReference = Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi.ProjectReference;
using Mapster;

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

        public async Task<TeamProject> GetTeamProjectAsync(string projectName)
        {
            _logger.LogInformation($"Getting project {projectName}");
            using var projectClient = await _vssConnection.GetClientAsync<ProjectHttpClient>();

            var project = await projectClient.GetProject(projectName);
            return project;
        }

        public async Task ShareServiceEndpointsAsync(string adpProjectName, List<string> serviceConnections, TeamProjectReference onBoardProject)
        {
            var serviceEndpointClient = await _vssConnection.GetClientAsync<ServiceEndpointHttpClient>();

            _logger.LogInformation($"Getting service endpoints for project {adpProjectName}");

            var endpoints = await serviceEndpointClient.GetServiceEndpointsAsync(adpProjectName);

            foreach (var serviceConnection in serviceConnections)
            {
                var endpoint = endpoints.FirstOrDefault(e => e.Name.Equals(serviceConnection, StringComparison.OrdinalIgnoreCase));

                if (endpoint != null)
                {
                    var isAlreadyShared = endpoint.ServiceEndpointProjectReferences.Any(r => r.ProjectReference.Id == onBoardProject.Id);
                    if (!isAlreadyShared)
                    {
                        _logger.LogInformation($"Sharing service endpoint {serviceConnection} with project {onBoardProject.Name}");

                        var serviceEndpointProjectReferences = new List<ServiceEndpointProjectReference>() {
                            new() { Name = onBoardProject.Name,ProjectReference = onBoardProject.Adapt<ProjectReference>() }
                        };

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

        public async Task AddEnvironmentsAsync(List<AdoEnvironment> adoEnvironments, TeamProjectReference onBoardProject)
        {
            var taskAgentClient = await _vssConnection.GetClientAsync<TaskAgentHttpClient>();

            _logger.LogInformation($"Getting environments for project {onBoardProject.Name}");

            var environments = await taskAgentClient.GetEnvironmentsAsync(onBoardProject.Id);

            foreach (var environment in adoEnvironments)
            {
                var IsEnvironmentExists = environments.Any(e => e.Name.Equals(environment.Name, StringComparison.OrdinalIgnoreCase));

                if (IsEnvironmentExists)
                {
                    _logger.LogInformation($"Environment {environment.Name} already exists");
                    continue;
                }

                _logger.LogInformation($"Creating environment {environment.Name}");

                var environmentParameter = environment.Adapt<EnvironmentCreateParameter>();

                await taskAgentClient.AddEnvironmentAsync(onBoardProject.Id, environmentParameter);

                _logger.LogInformation($"Environment {environment.Name} created");
            }
        }

        public async Task ShareAgentPoolsAsync(string adpPrjectName, List<string> adoAgentPoolsToShare, TeamProjectReference onBoardProject)
        {
            var taskAgentClient = await _vssConnection.GetClientAsync<TaskAgentHttpClient>();

            _logger.LogInformation($"Getting agent pools for project {onBoardProject.Name}");

            var adpAgentQueues = await taskAgentClient.GetAgentQueuesAsync(adpPrjectName, string.Empty);

            var agentPools = await taskAgentClient.GetAgentQueuesAsync(onBoardProject.Id);

            foreach (var agentPool in adoAgentPoolsToShare)
            {
                var adpAgentQueue = adpAgentQueues.FirstOrDefault(a => a.Name.Equals(agentPool, StringComparison.OrdinalIgnoreCase));
                if (adpAgentQueue != null)
                {
                    var IsAgentPoolExists = agentPools.Any(e => e.Name.Equals(agentPool, StringComparison.OrdinalIgnoreCase));

                    if (IsAgentPoolExists)
                    {
                        _logger.LogInformation($"Agent pool {agentPool} already exists in the {onBoardProject.Name} project");
                        continue;
                    }

                    _logger.LogInformation($"Adding agent pool {agentPool} to the {onBoardProject.Name} project");

                    await taskAgentClient.AddAgentQueueAsync(onBoardProject.Id, adpAgentQueue);

                    _logger.LogInformation($"Agent pool {agentPool} created");
                }
                else
                {
                    _logger.LogWarning($"Agent pool {agentPool} not found in the adp project.");
                }
            }
        }

        public async Task AddOrUpdateVariableGroupsAsync(List<AdoVariableGroup> adoVariableGroups, TeamProjectReference onBoardProject)
        {
            var taskAgentClient = await _vssConnection.GetClientAsync<TaskAgentHttpClient>();

            _logger.LogInformation($"Getting variable groups for project {onBoardProject.Name}");

            var variableGroups = await taskAgentClient.GetVariableGroupsAsync(onBoardProject.Id);

            foreach (var variableGroup in adoVariableGroups)
            {
                var existingVariableGroup = variableGroups.First(e => e.Name.Equals(variableGroup.Name, StringComparison.OrdinalIgnoreCase));
                
                var variableGroupParameters = new VariableGroupParameters();
                variableGroupParameters = variableGroup.Adapt<VariableGroupParameters>();
                variableGroupParameters.VariableGroupProjectReferences[0].ProjectReference = onBoardProject.Adapt<Microsoft.TeamFoundation.DistributedTask.WebApi.ProjectReference>();

                if (existingVariableGroup == null)
                {
                    _logger.LogInformation($"Creating variable group {variableGroup.Name}");
                    await taskAgentClient.AddVariableGroupAsync(variableGroupParameters);
                }
                else
                {
                    _logger.LogInformation($"Updating variable group {variableGroup.Name}");
                    await taskAgentClient.UpdateVariableGroupAsync(existingVariableGroup.Id, variableGroupParameters);
                }
            }

        }
    }
}
