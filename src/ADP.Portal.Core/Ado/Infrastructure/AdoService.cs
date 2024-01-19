using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints;
using ADP.Portal.Core.Ado.Entities;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using ProjectReference = Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi.ProjectReference;
using DistributedTaskProjectReference = Microsoft.TeamFoundation.DistributedTask.WebApi.ProjectReference;
using Mapster;

namespace ADP.Portal.Core.Ado.Infrastructure
{
    public class AdoService : IAdoService
    {
        private readonly ILogger<AdoService> logger;
        private readonly IVssConnection vssConnection;

        public AdoService(ILogger<AdoService> logger, Task<IVssConnection> vssConnection)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.vssConnection = vssConnection.Result;
        }

        public async Task<TeamProject> GetTeamProjectAsync(string projectName)
        {
            logger.LogInformation($"Getting project {projectName}");
            using var projectClient = await vssConnection.GetClientAsync<ProjectHttpClient>();

            var project = await projectClient.GetProject(projectName);
            return project;
        }

        public async Task ShareServiceEndpointsAsync(string adpProjectName, List<string> serviceConnections, TeamProjectReference onBoardProject)
        {
            var serviceEndpointClient = await vssConnection.GetClientAsync<ServiceEndpointHttpClient>();

            logger.LogInformation($"Getting service endpoints for project {adpProjectName}");

            var endpoints = await serviceEndpointClient.GetServiceEndpointsAsync(adpProjectName);

            foreach (var serviceConnection in serviceConnections)
            {
                var endpoint = endpoints.FirstOrDefault(e => e.Name.Equals(serviceConnection, StringComparison.OrdinalIgnoreCase));

                if (endpoint != null)
                {
                    var isAlreadyShared = endpoint.ServiceEndpointProjectReferences.Any(r => r.ProjectReference.Id == onBoardProject.Id);
                    if (!isAlreadyShared)
                    {
                        logger.LogInformation($"Sharing service endpoint {serviceConnection} with project {onBoardProject.Name}");

                        var serviceEndpointProjectReferences = new List<ServiceEndpointProjectReference>() {
                            new() { Name = onBoardProject.Name,ProjectReference = onBoardProject.Adapt<ProjectReference>() }
                        };

                        await serviceEndpointClient.ShareServiceEndpointAsync(endpoint.Id, serviceEndpointProjectReferences);
                    }
                    else
                    {
                        logger.LogInformation($"Service endpoint {serviceConnection} already shared with project {onBoardProject.Name}");
                    }
                }
                else
                {
                    logger.LogWarning($"Service endpoint {serviceConnection} not found");
                }
            }
        }

        public async Task AddEnvironmentsAsync(List<AdoEnvironment> adoEnvironments, TeamProjectReference onBoardProject)
        {
            var taskAgentClient = await vssConnection.GetClientAsync<TaskAgentHttpClient>();

            logger.LogInformation($"Getting environments for project {onBoardProject.Name}");

            var environments = await taskAgentClient.GetEnvironmentsAsync(onBoardProject.Id);

            foreach (var environment in adoEnvironments)
            {
                var IsEnvironmentExists = environments.Any(e => e.Name.Equals(environment.Name, StringComparison.OrdinalIgnoreCase));

                if (IsEnvironmentExists)
                {
                    logger.LogInformation($"Environment {environment.Name} already exists");
                    continue;
                }

                logger.LogInformation($"Creating environment {environment.Name}");

                var environmentParameter = environment.Adapt<EnvironmentCreateParameter>();

                await taskAgentClient.AddEnvironmentAsync(onBoardProject.Id, environmentParameter);

                logger.LogInformation($"Environment {environment.Name} created");
            }
        }

        public async Task ShareAgentPoolsAsync(string adpPrjectName, List<string> adoAgentPoolsToShare, TeamProjectReference onBoardProject)
        {
            var taskAgentClient = await vssConnection.GetClientAsync<TaskAgentHttpClient>();

            logger.LogInformation($"Getting agent pools for project {onBoardProject.Name}");

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
                        logger.LogInformation($"Agent pool {agentPool} already exists in the {onBoardProject.Name} project");
                        continue;
                    }

                    logger.LogInformation($"Adding agent pool {agentPool} to the {onBoardProject.Name} project");

                    await taskAgentClient.AddAgentQueueAsync(onBoardProject.Id, adpAgentQueue);

                    logger.LogInformation($"Agent pool {agentPool} created");
                }
                else
                {
                    logger.LogWarning($"Agent pool {agentPool} not found in the adp project.");
                }
            }
        }

        public async Task AddOrUpdateVariableGroupsAsync(List<AdoVariableGroup> adoVariableGroups, TeamProjectReference onBoardProject)
        {
            var taskAgentClient = await vssConnection.GetClientAsync<TaskAgentHttpClient>();

            logger.LogInformation($"Getting variable groups for project {onBoardProject.Name}");

            var variableGroups = await taskAgentClient.GetVariableGroupsAsync(onBoardProject.Id);

            foreach (var variableGroup in adoVariableGroups)
            {
                var existingVariableGroup = variableGroups.First(e => e.Name.Equals(variableGroup.Name, StringComparison.OrdinalIgnoreCase));
                
                var variableGroupParameters = new VariableGroupParameters();
                variableGroupParameters = variableGroup.Adapt<VariableGroupParameters>();
                variableGroupParameters.VariableGroupProjectReferences[0].ProjectReference = onBoardProject.Adapt<DistributedTaskProjectReference>();

                if (existingVariableGroup == null)
                {
                    logger.LogInformation($"Creating variable group {variableGroup.Name}");
                    await taskAgentClient.AddVariableGroupAsync(variableGroupParameters);
                }
                else
                {
                    logger.LogInformation($"Updating variable group {variableGroup.Name}");
                    await taskAgentClient.UpdateVariableGroupAsync(existingVariableGroup.Id, variableGroupParameters);
                }
            }
        }
    }
}
