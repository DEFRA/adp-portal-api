using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.Extensions.Logging;
using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Infrastructure;

namespace ADP.Portal.Core.Ado.Services
{
    public class AdoProjectService : IAdoProjectService
    {
        private readonly ILogger<AdoProjectService> _logger;
        private readonly IAdoService _adoService;

        public AdoProjectService(IAdoService adoService, ILogger<AdoProjectService> logger)
        {
            _adoService = adoService;
            _logger = logger;
        }

        public async Task<TeamProjectReference?> GetProjectAsync(string projectName)
        {
            try
            {
                return await _adoService.GetTeamProjectAsync(projectName);
            }
            catch (ProjectDoesNotExistWithNameException)
            {
                _logger.LogWarning($"Project {projectName} does not exist");
                return null;
            }
        }

        public async Task OnBoardAsync(string adpProjectName, AdoProject onboardProject)
        {
            await _adoService.ShareServiceEndpointsAsync(adpProjectName, onboardProject.ServiceConnections, onboardProject.ProjectReference);

            await _adoService.AddEnvironmentsAsync(onboardProject.Environments, onboardProject.ProjectReference);

            await _adoService.ShareAgentPoolsAsync(adpProjectName, onboardProject.AgentPools, onboardProject.ProjectReference);

            if(onboardProject.VariableGroups != null)
            {
                await _adoService.AddOrUpdateVariableGroupsAsync(onboardProject.VariableGroups, onboardProject.ProjectReference);
            }
        }
    }
}
