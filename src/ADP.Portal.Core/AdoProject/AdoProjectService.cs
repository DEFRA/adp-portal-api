using ADP.Portal.Core.Interfaces;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace ADP.Portal.Core.AdoProject
{
    public class AdoProjectService : IAdoProjectService
    {
        private readonly VssConnection vssConnection;

        public AdoProjectService(Task<VssConnection> vssConnection)
        {
            this.vssConnection = vssConnection.Result;
        }

        public async Task<Guid> GetProjectAsync(string projectName)
        {

            var projectClient = await vssConnection.GetClientAsync<ProjectHttpClient>();

            var projects = await projectClient.GetProjects();

            return await Task.FromResult(Guid.NewGuid());
        }
    }
}
