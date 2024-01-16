using ADP.Portal.Core.Domain;
using Microsoft.TeamFoundation.Core.WebApi;

namespace ADP.Portal.Core.Application
{
    public interface IAdoProjectService
    {
        public Task<TeamProjectReference?> GetProjectAsync(string projectName);

        public Task OnBoardAsync(TeamProjectReference onBoardProject, AdoProject adpProject);

    }
}
