using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Extensions;
using ADP.Portal.Core.Git.Infrastructure;
using ADP.Portal.Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Services.Common;
using Octokit;
using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Git.Services
{
    public class FluxTeamConfigService : IFluxTeamConfigService
    {
        private readonly IGitHubRepository gitHubRepository;
        private readonly ICacheService cacheService;
        private readonly GitRepo teamGitRepo;
        private readonly GitRepo fluxServiceRepo;
        private readonly GitRepo fluxTemplatesRepo;
        private readonly ILogger<FluxTeamConfigService> logger;
        private readonly ISerializer serializer;

        public FluxTeamConfigService(IGitHubRepository gitHubRepository, IOptionsSnapshot<GitRepo> gitRepoOptions, ICacheService cacheService,
            ILogger<FluxTeamConfigService> logger, ISerializer serializer)
        {
            this.gitHubRepository = gitHubRepository;
            this.cacheService = cacheService;
            this.teamGitRepo = gitRepoOptions.Get(Constants.GitRepo.TEAM_REPO_CONFIG);
            this.fluxServiceRepo = gitRepoOptions.Get(Constants.GitRepo.TEAM_FLUX_SERVICES_CONFIG);
            this.fluxTemplatesRepo = gitRepoOptions.Get(Constants.GitRepo.TEAM_FLUX_TEMPLATES_CONFIG);
            this.logger = logger;
            this.serializer = serializer;
        }

        public async Task<T?> GetConfigAsync<T>(string? tenantName = null, string? teamName = null)
        {
            try
            {
                var isTenant = !string.IsNullOrEmpty(tenantName);
                var name = isTenant ? tenantName : teamName;
                var pathFormat = isTenant ? Constants.Flux.GIT_REPO_TENANT_CONFIG_PATH : Constants.Flux.GIT_REPO_TEAM_CONFIG_PATH;
                var path = string.Format(pathFormat, name);
        
                logger.LogInformation(isTenant ? "Reading flux team config for the tenant:'{TenantName}'." : "Reading flux team config for the team:'{TeamName}'.", name);
                return await gitHubRepository.GetConfigAsync<T>(path, teamGitRepo);
            }
            catch (NotFoundException)
            {
                return default;
            }
        }

        public async Task<FluxConfigResult> CreateConfigAsync(string teamName, FluxTeamConfig fluxTeamConfig)
        {
            var result = new FluxConfigResult();

            logger.LogInformation("Creating flux team config for the team:'{TeamName}'.", teamName);
            var response = await gitHubRepository.CreateConfigAsync(teamGitRepo, string.Format(Constants.Flux.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(fluxTeamConfig));
            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }

        public async Task<FluxConfigResult> UpdateConfigAsync(string teamName, FluxTeamConfig fluxTeamConfig)
        {
            var result = new FluxConfigResult() { IsConfigExists = false };

            var existingConfig = await GetConfigAsync<FluxTeamConfig>(teamName: teamName);
            if (existingConfig != null)
            {
                result.IsConfigExists = true;
                logger.LogInformation("Updating flux team config for the team:'{TeamName}'.", teamName);
                var response = await gitHubRepository.UpdateConfigAsync(teamGitRepo, string.Format(Constants.Flux.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(fluxTeamConfig));
                if (string.IsNullOrEmpty(response))
                {
                    result.Errors.Add($"Failed to save the config for the team: {teamName}");
                }
            }

            return result;
        }

        public async Task<GenerateFluxConfigResult> GenerateConfigAsync(string tenantName, string teamName, string? serviceName = null, string? environment = null)
        {
            var result = new GenerateFluxConfigResult();

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(teamName: teamName);
            var tenantConfig = await GetConfigAsync<FluxTenant>(tenantName: tenantName);

            if (teamConfig == null || tenantConfig == null)
            {
                logger.LogWarning("Flux team config not found for the team:'{TeamName}'.", teamName);
                result.IsConfigExists = false;
                return result;
            }

            logger.LogInformation("Reading flux templates.");
            var cacheKey = $"flux-templates-{fluxTemplatesRepo.Reference}";
            var templates = cacheService.Get<IEnumerable<KeyValuePair<string, Dictionary<object, object>>>>(cacheKey);
            if (templates == null)
            {
                templates = await gitHubRepository.GetAllFilesAsync(fluxTemplatesRepo, Constants.Flux.GIT_REPO_TEMPLATE_PATH);
                cacheService.Set(cacheKey, templates);
            }

            logger.LogInformation("Generating flux config for the team:'{TeamName}', service:'{ServiceName}' and environment:'{Environment}'.", teamName, serviceName, environment);
            var generatedFiles = ProcessTemplates(templates, tenantConfig, teamConfig, serviceName, environment);

            if (generatedFiles.Count > 0) await PushFilesToFluxRepository(fluxServiceRepo, teamName, serviceName, generatedFiles);

            return result;
        }

        public async Task<FluxConfigResult> AddServiceAsync(string teamName, FluxService fluxService)
        {
            var result = new FluxConfigResult() { IsConfigExists = false };

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(teamName: teamName);
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
            var response = await gitHubRepository.UpdateConfigAsync(teamGitRepo, string.Format(Constants.Flux.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(teamConfig));

            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }

        public async Task<FluxConfigResult> AddServiceEnvironmentAsync(string teamName, string serviceName, FluxEnvironment newEnvironment)
        {
            var result = new FluxConfigResult() { IsConfigExists = false };

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(teamName: teamName);
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
            var response = await gitHubRepository.UpdateConfigAsync(teamGitRepo, string.Format(Constants.Flux.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(teamConfig));

            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }

        #region Private methods

        private static Dictionary<string, Dictionary<object, object>> ProcessTemplates(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> files,
            FluxTenant tenantConfig, FluxTeamConfig fluxTeamConfig, string? serviceName = null, string? environment = null)
        {
            var finalFiles = new Dictionary<string, Dictionary<object, object>>();

            var services = serviceName != null ? fluxTeamConfig.Services.Where(x => x.Name.Equals(serviceName)) : fluxTeamConfig.Services;
            if (services.Any())
            {
                if (!string.IsNullOrEmpty(environment))
                {
                    foreach (var service in services)
                    {
                        service.Environments = service.Environments.Where(env => env.Name.Equals(environment)).ToList();
                    }

                    foreach (var service in fluxTeamConfig.Services)
                    {
                        service.Environments = service.Environments.Where(env => env.Name.Equals(environment)).ToList();
                    }
                }

                // Create service files
                finalFiles = CreateServices(files, tenantConfig, fluxTeamConfig, services);

                // Replace tokens
                CreateTeamVariables(fluxTeamConfig);
                fluxTeamConfig.ConfigVariables.Union(tenantConfig.ConfigVariables).ForEach(finalFiles.ReplaceToken);
            }

            return finalFiles;
        }

        private static Dictionary<string, Dictionary<object, object>> CreateServices(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> templates,
            FluxTenant tenantConfig, FluxTeamConfig teamConfig, IEnumerable<FluxService> services)
        {
            var finalFiles = new Dictionary<string, Dictionary<object, object>>();

            // Collect all non-service files
            templates.Where(x => !x.Key.StartsWith(Constants.Flux.SERVICE_FOLDER) &&
                             !x.Key.StartsWith(Constants.Flux.TEAM_ENV_FOLDER)).ForEach(file =>
                             {
                                 var key = file.Key.Replace(Constants.Flux.PROGRAMME_FOLDER, teamConfig.ProgrammeName).Replace(Constants.Flux.TEAM_KEY, teamConfig.TeamName);
                                 finalFiles.Add($"services/{key}", file.Value);
                             });

            // Create team environments
            var envTemplates = templates.Where(x => x.Key.Contains(Constants.Flux.TEAM_ENV_FOLDER));
            finalFiles.AddRange(CreateEnvironmentFiles(envTemplates, tenantConfig, teamConfig, teamConfig.Services));

            // Create files for each service
            var serviceTemplates = templates.Where(x => x.Key.StartsWith(Constants.Flux.SERVICE_FOLDER)).ToList();
            foreach (var service in services)
            {
                var serviceFiles = new Dictionary<string, Dictionary<object, object>>();
                var serviceTypeBasedTemplates = ServiceTypeBasedFiles(serviceTemplates, service);

                foreach (var template in serviceTypeBasedTemplates)
                {
                    if (!template.Key.Contains(Constants.Flux.ENV_KEY))
                    {
                        var serviceTemplate = template.Value.DeepCopy();
                        if (template.Key.Equals(Constants.Flux.TEAM_SERVICE_KUSTOMIZATION_FILE) && service.HasDatastore())
                        {
                            ((List<object>)serviceTemplate[Constants.Flux.RESOURCES_KEY]).Add("pre-deploy-kustomize.yaml");
                        }
                        var key = template.Key.Replace(Constants.Flux.PROGRAMME_FOLDER, teamConfig.ProgrammeName).Replace(Constants.Flux.TEAM_KEY, teamConfig.TeamName).Replace(Constants.Flux.SERVICE_KEY, service.Name);
                        serviceFiles.Add($"services/{key}", serviceTemplate);
                    }
                    else
                    {
                        serviceFiles.AddRange(CreateEnvironmentFiles([template], tenantConfig, teamConfig, [service]));
                    }
                }
                UpdateServicePatchFiles(serviceFiles, service, teamConfig);

                service.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.TEMPLATE_VAR_DEPENDS_ON, Value = service.HasDatastore() ? Constants.Flux.PREDEPLOY_KEY : Constants.Flux.INFRA_KEY });
                service.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.TEMPLATE_VAR_SERVICE_NAME, Value = service.Name });
                service.ConfigVariables.ForEach(serviceFiles.ReplaceToken);
                finalFiles.AddRange(serviceFiles);
            }

            return finalFiles;
        }

        private static IEnumerable<KeyValuePair<string, Dictionary<object, object>>> ServiceTypeBasedFiles(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> serviceTemplates, FluxService service)
        {
            return serviceTemplates.Where(filter =>
            {
                var matched = true;
                if (!service.HasDatastore())
                {
                    matched = !filter.Key.StartsWith(Constants.Flux.SERVICE_PRE_DEPLOY_FOLDER) && !filter.Key.StartsWith(Constants.Flux.PRE_DEPLOY_KUSTOMIZE_FILE);
                }
                return matched;
            });
        }

        private static Dictionary<string, Dictionary<object, object>> CreateEnvironmentFiles(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> templates, FluxTenant tenantConfig, FluxTeamConfig teamConfig, IEnumerable<FluxService> services)
        {
            var finalFiles = new Dictionary<string, Dictionary<object, object>>();

            foreach (var service in services)
            {
                foreach (var template in templates)
                {
                    service.Environments.Where(env => tenantConfig.Environments.Exists(x => x.Name.Equals(env.Name)))
                        .ForEach(environment =>
                        {
                            var key = template.Key.Replace(Constants.Flux.PROGRAMME_FOLDER, teamConfig.ProgrammeName)
                                .Replace(Constants.Flux.TEAM_KEY, teamConfig.TeamName)
                                .Replace(Constants.Flux.ENV_KEY, $"{environment.Name[..3]}/0{environment.Name[3..]}")
                                .Replace(Constants.Flux.SERVICE_KEY, service.Name);
                            key = $"services/{key}";

                            if (template.Key.Equals(Constants.Flux.TEAM_ENV_KUSTOMIZATION_FILE, StringComparison.InvariantCultureIgnoreCase) &&
                                finalFiles.TryGetValue(key, out var existingEnv))
                            {
                                ((List<object>)existingEnv[Constants.Flux.RESOURCES_KEY]).Add($"../../{service.Name}");
                            }
                            else
                            {
                                var newFile = template.Value.DeepCopy();
                                if (template.Key.Equals(Constants.Flux.TEAM_ENV_KUSTOMIZATION_FILE, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    ((List<object>)newFile[Constants.Flux.RESOURCES_KEY]).Add($"../../{service.Name}");
                                }

                                var tokens = new List<FluxConfig>
                            {
                                new() { Key = Constants.Flux.TEMPLATE_VAR_VERSION, Value = Constants.Flux.TEMPLATE_VAR_DEFAULT_VERSION },
                                new() { Key = Constants.Flux.TEMPLATE_VAR_VERSION_TAG, Value = Constants.Flux.TEMPLATE_VAR_DEFAULT_VERSION_TAG },
                                new() { Key = Constants.Flux.TEMPLATE_VAR_MIGRATION_VERSION, Value = Constants.Flux.TEMPLATE_VAR_DEFAULT_MIGRATION_VERSION },
                                new() { Key = Constants.Flux.TEMPLATE_VAR_MIGRATION_VERSION_TAG, Value = Constants.Flux.TEMPLATE_VAR_DEFAULT_MIGRATION_VERSION_TAG },
                                new() { Key = Constants.Flux.TEMPLATE_VAR_PS_EXEC_VERSION, Value = Constants.Flux.TEMPLATE_VAR_PS_EXEC_DEFAULT_VERSION }
                            };
                                tokens.ForEach(newFile.ReplaceToken);

                                tokens =
                                [
                                    new() { Key = Constants.Flux.TEMPLATE_VAR_ENVIRONMENT, Value = environment.Name[..3]},
                                new() { Key = Constants.Flux.TEMPLATE_VAR_ENV_INSTANCE, Value = environment.Name[3..]},
                            ];
                                var tenantConfigVariables = tenantConfig.Environments.First(x => x.Name.Equals(environment.Name)).ConfigVariables ?? [];

                                tokens.Union(environment.ConfigVariables).Union(tenantConfigVariables).ForEach(newFile.ReplaceToken);
                                finalFiles.Add(key, newFile);
                            }
                        });
                }
            }
            return finalFiles;
        }

        private static void UpdateServicePatchFiles(Dictionary<string, Dictionary<object, object>> serviceFiles, FluxService service, FluxTeamConfig teamConfig)
        {
            foreach (var file in serviceFiles)
            {
                service.Environments.ForEach(env =>
                {
                    var filePattern = string.Format(Constants.Flux.TEAM_SERVICE_DEPLOY_ENV_PATCH_FILE, teamConfig.ProgrammeName, teamConfig.TeamName, service.Name, $"{env.Name[..3]}/0{env.Name[3..]}");
                    if (service.Type.Equals(FluxServiceType.Backend) && file.Key.Equals(filePattern))
                    {
                        new YamlQuery(file.Value)
                            .On(Constants.Flux.SPEC_KEY)
                            .On(Constants.Flux.VALUES_KEY)
                            .Remove(Constants.Flux.LABELS_KEY)
                            .Remove(Constants.Flux.INGRESS_KEY);
                    }
                    filePattern = string.Format(Constants.Flux.TEAM_SERVICE_INFRA_ENV_PATCH_FILE, teamConfig.ProgrammeName, teamConfig.TeamName, service.Name, $"{env.Name[..3]}/0{env.Name[3..]}");
                    if (service.Type.Equals(FluxServiceType.Frontend) && file.Key.Equals(filePattern))
                    {
                        new YamlQuery(file.Value)
                            .On(Constants.Flux.SPEC_KEY)
                            .On(Constants.Flux.VALUES_KEY)
                            .Remove(Constants.Flux.POSTGRESRESOURCEGROUPNAME_KEY)
                            .Remove(Constants.Flux.POSTGRESSERVERNAME_KEY);
                    }
                });
            }
        }

        private static void CreateTeamVariables(FluxTeamConfig teamConfig)
        {
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.TEMPLATE_VAR_PROGRAMME_NAME, Value = teamConfig.ProgrammeName ?? string.Empty });
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.TEMPLATE_VAR_TEAM_NAME, Value = teamConfig.TeamName ?? string.Empty });
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.TEMPLATE_VAR_SERVICE_CODE, Value = teamConfig.ServiceCode ?? string.Empty });
        }

        private async Task PushFilesToFluxRepository(GitRepo gitRepoFluxServices, string teamName, string? serviceName, Dictionary<string, Dictionary<object, object>> generatedFiles)
        {
            var branchName = $"refs/heads/features/{teamName}{(string.IsNullOrEmpty(serviceName) ? "" : $"-{serviceName}")}";
            var branchRef = await gitHubRepository.GetBranchAsync(gitRepoFluxServices, branchName);

            string message;
            if (branchRef == null)
            {
                message = string.IsNullOrEmpty(serviceName) ? $"{teamName.ToUpper()} Config" : $"{serviceName.ToUpper()} Config";
            }
            else
            {
                message = "Update config";
            }

            logger.LogInformation("Creating commit for the branch:'{BranchName}'.", branchName);
            var commitRef = await gitHubRepository.CreateCommitAsync(gitRepoFluxServices, generatedFiles, message, branchRef == null ? null : branchName);

            if (commitRef != null)
            {
                if (branchRef == null)
                {
                    logger.LogInformation("Creating branch:'{BranchName}'.", branchName);
                    await gitHubRepository.CreateBranchAsync(gitRepoFluxServices, branchName, commitRef.Sha);
                    logger.LogInformation("Creating pull request for the branch:'{BranchName}'.", branchName);
                    await gitHubRepository.CreatePullRequestAsync(gitRepoFluxServices, branchName, message);
                }
                else
                {
                    logger.LogInformation("Updating branch:'{BranchName}' with the changes.", branchName);
                    await gitHubRepository.UpdateBranchAsync(gitRepoFluxServices, branchName, commitRef.Sha);
                }
            }
            else
            {
                logger.LogInformation("No changes found in the flux files for the team:'{TeamName}' or the service:{serviceName}.", teamName, serviceName);
            }
        }

        #endregion
    }
}
