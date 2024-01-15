using ADP.Portal.Core.Interfaces;
using Microsoft.VisualStudio.Services.WebApi;

namespace ADP.Portal.Core.AdoProject
{
    public class AdoProjectService : IAdoProjectService
    {
        private readonly Task<VssConnection> vssConnection;

        public AdoProjectService(Task<VssConnection> vssConnection)
        {
            this.vssConnection = vssConnection;
        }

        public async Task<Guid> GetProjectAsync(string projectName)
        {
            return await Task.FromResult(Guid.NewGuid());
        }
    }
}
