using ADP.Portal.Core.Ado.Entities;
using Microsoft.TeamFoundation.Core.WebApi;

namespace ADP.Portal.Core.Ado.Infrastructure
{
    public interface IAdoService
    {

        Task<TeamProject> GetTeamProjectAsync(string projectName);

        Task ShareServiceEndpointsAsync(AdoProject adpProject, TeamProjectReference onBoardProject);

        Task AddEnvironments(List<AdoEnvironment> adoEnvironments, TeamProjectReference onBoardProject);
    }
}