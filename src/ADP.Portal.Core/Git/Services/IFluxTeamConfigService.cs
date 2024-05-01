using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Services
{
    public interface IFluxTeamConfigService
    {
        Task<T?> GetConfigAsync<T>(string? tenantName = null, string? teamName = null);

        Task<FluxConfigResult> CreateConfigAsync(string teamName, FluxTeamConfig fluxTeamConfig);

        Task<FluxConfigResult> UpdateConfigAsync(string teamName, FluxTeamConfig fluxTeamConfig);

        Task<GenerateFluxConfigResult> GenerateConfigAsync(string tenantName, string teamName, string? serviceName = null, string? environment = null);

        Task<FluxConfigResult> AddServiceAsync(string teamName, FluxService fluxService);

        Task<FluxConfigResult> AddServiceEnvironmentAsync(string teamName, string serviceName, FluxEnvironment newEnvironment);
    }
}
