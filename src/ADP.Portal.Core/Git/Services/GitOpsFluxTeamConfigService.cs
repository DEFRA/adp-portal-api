using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using Microsoft.Extensions.Logging;
using Octokit;
using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Git.Services
{
    public class GitOpsFluxTeamConfigService : IGitOpsFluxTeamConfigService
    {
        private readonly IGitOpsConfigRepository gitOpsConfigRepository;
        private readonly ILogger<GitOpsFluxTeamConfigService> logger;
        private readonly ISerializer serializer;

        public GitOpsFluxTeamConfigService(IGitOpsConfigRepository gitOpsConfigRepository, ILogger<GitOpsFluxTeamConfigService> logger, ISerializer serializer)
        {
            this.gitOpsConfigRepository = gitOpsConfigRepository;
            this.logger = logger;
            this.serializer = serializer;
        }

        public async Task<T?> GetConfigAsync<T>(GitRepo gitRepo, string? tenantName = null, string? teamName = null)
        {
            try
            {
                var path = string.Empty;
                if (string.IsNullOrEmpty(tenantName))
                {
                    logger.LogInformation("Reading flux team config for the team:'{TeamName}'.", teamName);
                    path = string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName);
                }
                else
                {
                    logger.LogInformation("Reading flux team config for the tenant:'{TenantName}'.", tenantName);
                    path = string.Format(FluxConstants.GIT_REPO_TENANT_CONFIG_PATH, tenantName);
                }

                return await gitOpsConfigRepository.GetConfigAsync<T>(path, gitRepo);
            }
            catch (NotFoundException)
            {
                return default;
            }
        }

        public async Task<FluxConfigResult> CreateConfigAsync(GitRepo gitRepo, string teamName, FluxTeamConfig fluxTeamConfig)
        {
            var result = new FluxConfigResult();

            logger.LogInformation("Creating flux team config for the team:'{TeamName}'.", teamName);
            var response = await gitOpsConfigRepository.CreateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(fluxTeamConfig));
            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }

        public async Task<FluxConfigResult> UpdateConfigAsync(GitRepo gitRepo, string teamName, FluxTeamConfig fluxTeamConfig)
        {
            var result = new FluxConfigResult() { IsConfigExists = false };

            var existingConfig = await GetConfigAsync<FluxTeamConfig>(gitRepo, teamName: teamName);
            if (existingConfig != null)
            {
                result.IsConfigExists = true;
                logger.LogInformation("Updating flux team config for the team:'{TeamName}'.", teamName);
                var response = await gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(fluxTeamConfig));
                if (string.IsNullOrEmpty(response))
                {
                    result.Errors.Add($"Failed to save the config for the team: {teamName}");
                }
            }

            return result;
        }

        public async Task<GenerateFluxConfigResult> GenerateConfigAsync(GitRepo gitRepo, GitRepo gitRepoFluxServices, string tenantName, string teamName, string? serviceName = null)
        {
            var result = new GenerateFluxConfigResult();

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(gitRepo, teamName: teamName);
            var tenantConfig = await GetConfigAsync<FluxTenant>(gitRepo, tenantName: tenantName);

            if (teamConfig == null || tenantConfig == null)
            {
                logger.LogWarning("Flux team config not found for the team:'{TeamName}'.", teamName);
                result.IsConfigExists = false;
                return result;
            }

            logger.LogInformation("Reading flux templates.");
            var templates = await gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH);

            logger.LogInformation("Generating flux config for the team:'{TeamName}' and service:'{ServiceName}'.", teamName, serviceName);
            var generatedFiles = TemplateBuilder.ProcessTemplates(templates, tenantConfig, teamConfig, serviceName);

            var branchName = $"refs/heads/features/{teamName}{(string.IsNullOrEmpty(serviceName) ? "" : $"-{serviceName}")}";
            if (generatedFiles.Count > 0) await gitOpsConfigRepository.PushFilesToRepository(gitRepoFluxServices, branchName, generatedFiles);

            return result;
        }

        public async Task<FluxConfigResult> AddServiceAsync(GitRepo gitRepo, string teamName, FluxService fluxService)
        {
            var result = new FluxConfigResult() { IsConfigExists = false };

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(gitRepo, teamName: teamName);
            if (teamConfig == null)
            {
                return result;
            }

            result.IsConfigExists = true;

            if (teamConfig.Services.Exists(s => s.Name == fluxService.Name))
            {
                result.Errors.Add($"Service '{fluxService.Name}' already exists in the team:'{teamName}'.");
                logger.LogInformation("Service '{ServiceName}' already exists in the team: '{TeamName}'.", fluxService.Name, teamName);
                return result;
            }

            logger.LogInformation("Adding service '{ServiceName}' to the team:'{TeamName}'.", fluxService.Name, teamName);
            teamConfig.Services.Add(fluxService);
            var response = await gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(teamConfig));

            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }

        public async Task<FluxConfigResult> AddServiceEnvironmentAsync(GitRepo gitRepo, string teamName, string serviceName, FluxEnvironment newEnvironment)
        {
            var result = new FluxConfigResult() { IsConfigExists = false };

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(gitRepo, teamName: teamName);
            if (teamConfig == null)
            {
                logger.LogWarning("Flux team config not found for the team:'{TeamName}'.", teamName);
                result.Errors.Add($"Flux team config not found for the team:'{teamName}'.");
                return result;
            }

            var service = teamConfig.Services.Find(s => s.Name == serviceName);
            if (service == null)
            {
                logger.LogWarning("Service '{ServiceName}' not found in the team:'{TeamName}'.", serviceName, teamName);
                result.Errors.Add($"Service '{serviceName}' not found in the team:'{teamName}'.");
                return result;
            }

            result.IsConfigExists = true;
            if (service.Environments.Exists(e => e.Name == newEnvironment.Name))
            {
                return result;
            }

            service.Environments.Add(newEnvironment);

            logger.LogInformation("Adding environment '{EnvironmentName}' to the service:'{ServiceName}' in the team:'{TeamName}'.", newEnvironment.Name, serviceName, teamName);
            var response = await gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(teamConfig));

            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }
    }
}
