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

        public async Task OnBoardAsync(TeamProjectReference onBoardProject, AdoProject adpProject, List<AdoEnvironment> adoEnvironments)
        {
            await _adoService.ShareServiceEndpointsAsync(adpProject, onBoardProject);

            await _adoService.AddEnvironments(adoEnvironments, onBoardProject);
        }

    }
}
