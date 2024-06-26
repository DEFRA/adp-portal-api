﻿using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Extensions;
using ADP.Portal.Core.Git.Infrastructure;
using ADP.Portal.Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Services.Common;
using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Git.Services
{
    public class FluxTeamConfigService : IFluxTeamConfigService
    {
        private readonly IGitHubRepository gitHubRepository;
        private readonly IFluxTemplateService fluxTemplateService;
        private readonly GitRepo teamGitRepo;
        private readonly GitRepo fluxServiceRepo;
        private readonly GitRepo fluxTemplatesRepo;
        private readonly ILogger<FluxTeamConfigService> logger;
        private readonly ISerializer serializer;

        public FluxTeamConfigService(IGitHubRepository gitHubRepository, IOptionsSnapshot<GitRepo> gitRepoOptions, IFluxTemplateService fluxTemplateService,
            ILogger<FluxTeamConfigService> logger, ISerializer serializer)
        {
            this.gitHubRepository = gitHubRepository;
            this.teamGitRepo = gitRepoOptions.Get(Constants.GitRepo.TEAM_REPO_CONFIG);
            this.fluxServiceRepo = gitRepoOptions.Get(Constants.GitRepo.TEAM_FLUX_SERVICES_CONFIG);
            this.fluxTemplatesRepo = gitRepoOptions.Get(Constants.GitRepo.TEAM_FLUX_TEMPLATES_CONFIG);
            this.fluxTemplateService = fluxTemplateService;
            this.logger = logger;
            this.serializer = serializer;
        }

        public async Task<T?> GetConfigAsync<T>(string? tenantName = null, string? teamName = null)
        {
            var isTenant = !string.IsNullOrEmpty(tenantName);
            var name = isTenant ? tenantName : teamName;
            var pathFormat = isTenant ? Constants.Flux.Templates.GIT_REPO_TENANT_CONFIG_PATH : Constants.Flux.Templates.GIT_REPO_TEAM_CONFIG_PATH;
            var path = string.Format(pathFormat, name);

            if (isTenant)
            {
                logger.LogInformation("Reading flux team config for the tenant:'{TenantName}'.", name);
            }
            else
            {
                logger.LogInformation("Reading flux team config for the team:'{TeamName}'.", name);
            }

            return await gitHubRepository.GetFileContentAsync<T>(teamGitRepo, path);
        }

        public async Task<FluxConfigResult> CreateConfigAsync(string teamName, FluxTeamConfig fluxTeamConfig)
        {
            var result = new FluxConfigResult();

            logger.LogInformation("Creating flux team config for the team:'{TeamName}'.", teamName);

            fluxTeamConfig.Services.ForEach(service => service.Environments.ForEach(env => env.Manifest = new FluxManifest { Generate = true }));

            var response = await gitHubRepository.CreateFileAsync(teamGitRepo, string.Format(Constants.Flux.Templates.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(fluxTeamConfig));
            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }

        public async Task<GenerateManifestResult> GenerateManifestAsync(string tenantName, string teamName, string? serviceName = null, string? environment = null)
        {
            var result = new GenerateManifestResult();

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(teamName: teamName);
            var tenantConfig = await GetConfigAsync<FluxTenant>(tenantName: tenantName);

            if (teamConfig == null || tenantConfig == null)
            {
                logger.LogDebug(Constants.Logger.FLUX_TEAM_CONFIG_NOT_FOUND, teamName);
                result.IsConfigExists = false;
                return result;
            }

            foreach (var service in teamConfig.Services)
            {
                service.Environments = service.Environments.Where(env => tenantConfig.Environments.Exists(x => x.Name.Equals(env.Name))).ToList();
            }

            logger.LogInformation("Reading flux templates.");
            var templates = await fluxTemplateService.GetFluxTemplatesAsync();

            logger.LogInformation("Generating flux config for the team:'{TeamName}', service:'{ServiceName}' and environment:'{Environment}'.", teamName, serviceName, environment);
            var services = serviceName != null ? teamConfig.Services.Where(x => x.Name.Equals(serviceName)) : teamConfig.Services;

            if (services.Any())
            {
                if (!string.IsNullOrEmpty(environment))
                {
                    FilterEnvironmentsByName(services.ToList(), environment);
                }

                logger.LogDebug("Processing templates for the team:'{TeamName}', service:'{ServiceName}' and environment:'{Environment}'.", teamName, serviceName, environment);
                var generatedFiles = ProcessTemplates(templates.DeepCopy(), tenantConfig, teamConfig, services);

                if (generatedFiles.Count > 0)
                {
                    logger.LogDebug("Merging manifests for the team:'{TeamName}', service:'{ServiceName}' and environment:'{Environment}'.", teamName, serviceName, environment);
                    await MergeEnvServicesManifestsAsync(teamConfig.ProgrammeName, teamConfig.TeamName, services, generatedFiles);

                    await MergeEnvTeamsManifestsAsync(teamConfig.ProgrammeName, teamConfig.TeamName, services, generatedFiles);

                    logger.LogDebug("Pushing manifests to the repository:{FluxServiceRepo} for the team:'{TeamName}', service:'{ServiceName}' and environment:'{Environment}'.", fluxServiceRepo.Name, teamName, serviceName, environment);
                    await PushFilesToFluxRepository(fluxServiceRepo, teamName, serviceName, environment, generatedFiles);
                }
            }

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
                logger.LogDebug("Service '{ServiceName}' already exists in the team: '{TeamName}'.", fluxService.Name, teamName);
                result.Errors.Add($"Service '{fluxService.Name}' already exists in the team:'{teamName}'.");
                return result;
            }

            logger.LogInformation("Adding service '{ServiceName}' to the team:'{TeamName}'.", fluxService.Name, teamName);

            fluxService.Environments.ForEach(e => e.Manifest = new FluxManifest { Generate = true });

            if (fluxService.Type == FluxServiceType.Frontend && !fluxService.ConfigVariables.Exists(config => config.Key == Constants.Flux.Templates.INGRESS_ENDPOINT_TOKEN_KEY))
            {
                fluxService.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.Templates.INGRESS_ENDPOINT_TOKEN_KEY, Value = fluxService.Name });
            }

            teamConfig.Services.Add(fluxService);
            var response = await gitHubRepository.UpdateFileAsync(teamGitRepo, string.Format(Constants.Flux.Templates.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(teamConfig));

            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }

        public async Task<ServiceEnvironmentResult> GetServiceEnvironmentAsync(string teamName, string serviceName, string environment)
        {
            var result = new ServiceEnvironmentResult() { IsConfigExists = false, FluxTemplatesVersion = fluxTemplatesRepo.Reference };

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(teamName: teamName);
            if (teamConfig == null)
            {
                logger.LogDebug(Constants.Logger.FLUX_TEAM_CONFIG_NOT_FOUND, teamName);
                return result;
            }

            var service = teamConfig.Services.Find(s => s.Name == serviceName);
            if (service == null)
            {
                logger.LogDebug("Service '{ServiceName}' not found in the team:'{TeamName}'.", serviceName, teamName);
                return result;
            }

            var env = service.Environments.Find(e => e.Name == environment);
            if (env == null)
            {
                logger.LogDebug("Environment '{EnvironmentName}' not found for the service:'{ServiceName}' in the team:'{TeamName}'.", environment, serviceName, teamName);
                return result;
            }

            result.IsConfigExists = true;

            result.Environment = service.Environments.First(e => e.Name == environment);

            return result;
        }

        public async Task<FluxConfigResult> AddServiceEnvironmentAsync(string teamName, string serviceName, string environment)
        {
            var result = new FluxConfigResult() { IsConfigExists = false };

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(teamName: teamName);
            if (teamConfig == null)
            {
                logger.LogDebug(Constants.Logger.FLUX_TEAM_CONFIG_NOT_FOUND, teamName);
                result.Errors.Add($"Flux team config not found for the team:'{teamName}'.");
                return result;
            }

            var service = teamConfig.Services.Find(s => s.Name == serviceName);
            if (service == null)
            {
                logger.LogDebug("Service '{ServiceName}' not found in the team:'{TeamName}'.", serviceName, teamName);
                result.Errors.Add($"Service '{serviceName}' not found in the team:'{teamName}'.");
                return result;
            }

            result.IsConfigExists = true;
            if (service.Environments.Exists(e => e.Name == environment))
            {
                return result;
            }

            var newEnvironment = new FluxEnvironment { Name = environment, Manifest = new FluxManifest { Generate = true } };
            service.Environments.Add(newEnvironment);

            logger.LogInformation("Adding environment '{EnvironmentName}' to the service:'{ServiceName}' in the team:'{TeamName}'.", newEnvironment.Name, serviceName, teamName);
            var response = await gitHubRepository.UpdateFileAsync(teamGitRepo, string.Format(Constants.Flux.Templates.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(teamConfig));

            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }

        public async Task<FluxConfigResult> UpdateServiceEnvironmentManifestAsync(string teamName, string serviceName, string environment, bool generate)
        {
            var result = new FluxConfigResult() { IsConfigExists = false };

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(teamName: teamName);
            if (teamConfig == null)
            {
                logger.LogDebug(Constants.Logger.FLUX_TEAM_CONFIG_NOT_FOUND, teamName);
                result.Errors.Add($"Flux team config not found for the team:'{teamName}'.");
                return result;
            }

            var service = teamConfig.Services.Find(s => s.Name == serviceName);
            if (service == null)
            {
                logger.LogDebug("Service '{ServiceName}' not found in the team:'{TeamName}'.", serviceName, teamName);
                result.Errors.Add($"Service '{serviceName}' not found in the team:'{teamName}'.");
                return result;
            }

            var env = service.Environments.Find(e => e.Name == environment);
            if (env == null)
            {
                logger.LogDebug("Environment '{EnvironmentName}' not found for the service:'{ServiceName}' in the team:'{TeamName}'.", environment, serviceName, teamName);
                result.Errors.Add($"Environment '{environment}' not found for the service:'{serviceName}' in the team:'{teamName}'.");
                return result;
            }

            result.IsConfigExists = true;

            logger.LogInformation("Updating manifest for the environment '{EnvironmentName}' for the service:'{ServiceName}' in the team:'{TeamName}'.", environment, serviceName, teamName);
            env.Manifest ??= new FluxManifest() { Generate = generate };
            env.Manifest.Generate = generate;
            env.Manifest.GeneratedVersion = fluxTemplatesRepo.Reference;
            var response = await gitHubRepository.UpdateFileAsync(teamGitRepo, string.Format(Constants.Flux.Templates.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(teamConfig));

            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }

        #region Private methods


        private static Dictionary<string, FluxTemplateFile> ProcessTemplates(IEnumerable<KeyValuePair<string, FluxTemplateFile>> files,
            FluxTenant tenantConfig, FluxTeamConfig fluxTeamConfig, IEnumerable<FluxService> services)
        {
            var processedTemplates = CreateServices(files, tenantConfig, fluxTeamConfig, services);

            // Replace tokens
            CreateTeamVariables(fluxTeamConfig);

            fluxTeamConfig.ConfigVariables.Union(tenantConfig.ConfigVariables).ForEach(processedTemplates.ReplaceToken);

            return processedTemplates;
        }

        private static Dictionary<string, FluxTemplateFile> CreateServices(IEnumerable<KeyValuePair<string, FluxTemplateFile>> templates,
            FluxTenant tenantConfig, FluxTeamConfig teamConfig, IEnumerable<FluxService> services)
        {
            var finalFiles = new Dictionary<string, FluxTemplateFile>();

            // Collect all non-service files
            templates.Where(x => !x.Key.StartsWith(Constants.Flux.Templates.SERVICE_FOLDER) && !x.Key.StartsWith(Constants.Flux.Templates.TEAM_ENV_FOLDER))
                     .ForEach(file =>
                     {
                         var key = file.Key.Replace(Constants.Flux.Templates.PROGRAMME_FOLDER, teamConfig.ProgrammeName).Replace(Constants.Flux.Templates.TEAM_KEY, teamConfig.TeamName);
                         finalFiles.Add($"services/{key}", file.Value);
                     });

            // Create team environments
            var envTemplates = templates.Where(x => x.Key.Contains(Constants.Flux.Templates.TEAM_ENV_FOLDER));
            finalFiles.AddRange(CreateEnvironmentFiles(envTemplates, tenantConfig, teamConfig, services));

            // Create files for each service
            var serviceTemplates = templates.Where(x => x.Key.StartsWith(Constants.Flux.Templates.SERVICE_FOLDER)).ToList();
            foreach (var service in services)
            {
                var serviceFiles = new Dictionary<string, FluxTemplateFile>();
                var serviceTypeBasedTemplates = ServiceTypeBasedFiles(serviceTemplates, service);
                if (service.Type == FluxServiceType.HelmOnly)
                {
                    serviceTypeBasedTemplates = UpdateHelmDeployDirectoryNames(serviceTypeBasedTemplates);
                }
                foreach (var template in serviceTypeBasedTemplates)
                {
                    if (!template.Key.Contains(Constants.Flux.Templates.ENV_KEY))
                    {
                        var serviceTemplate = template.Value.DeepCopy();
                        serviceTemplate = UpdateServiceKustomizationFiles(serviceTemplate, template.Key, service);

                        var key = template.Key.Replace(Constants.Flux.Templates.PROGRAMME_FOLDER, teamConfig.ProgrammeName).Replace(Constants.Flux.Templates.TEAM_KEY, teamConfig.TeamName).Replace(Constants.Flux.Templates.SERVICE_KEY, service.Name);
                        serviceFiles.Add($"services/{key}", serviceTemplate);
                    }
                    else
                    {
                        serviceFiles.AddRange(CreateEnvironmentFiles([template], tenantConfig, teamConfig, [service]));
                    }
                }
                UpdateServicePatchFiles(serviceFiles, service, teamConfig);

                if (service.Type != FluxServiceType.HelmOnly)
                {
                    service.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.Templates.DEPENDS_ON_TOKEN, Value = service.HasDatastore() ? Constants.Flux.Templates.PREDEPLOY_KEY : Constants.Flux.Templates.INFRA_KEY });
                }
                service.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.Templates.SERVICE_NAME_TOKEN, Value = service.Name });
                service.ConfigVariables.ForEach(serviceFiles.ReplaceToken);
                finalFiles.AddRange(serviceFiles);
            }

            return finalFiles;
        }

        private static IEnumerable<KeyValuePair<string, FluxTemplateFile>> ServiceTypeBasedFiles(IEnumerable<KeyValuePair<string, FluxTemplateFile>> serviceTemplates, FluxService service)
        {
            return serviceTemplates.Where(filter =>
            {
                var matched = true;
                if (!service.HasDatastore())
                {
                    matched = !filter.Key.StartsWith(Constants.Flux.Templates.SERVICE_PRE_DEPLOY_FOLDER)
                                && !filter.Key.StartsWith(Constants.Flux.Templates.PRE_DEPLOY_KUSTOMIZE_FILE);
                }
                return matched;
            }).Where(filter =>
            {
                var matched = true;
                if (service.Type == FluxServiceType.HelmOnly)
                {
                    matched = (!filter.Key.StartsWith(Constants.Flux.Templates.SERVICE_DEPLOY_FOLDER) || filter.Key.Equals(Constants.Flux.Templates.DEPLOY_KUSTOMIZE_FILE))
                                && !filter.Key.StartsWith(Constants.Flux.Templates.SERVICE_INFRA_FOLDER)
                                && !filter.Key.StartsWith(Constants.Flux.Templates.INFRA_KUSTOMIZE_FILE);
                }
                else
                {
                    matched = !filter.Key.StartsWith(Constants.Flux.Templates.SERVICE_HELMONLY_DEPLOY_FOLDER);
                }
                return matched;
            });
        }

        private static Dictionary<string, FluxTemplateFile> CreateEnvironmentFiles(IEnumerable<KeyValuePair<string, FluxTemplateFile>> templates, FluxTenant tenantConfig, FluxTeamConfig teamConfig, IEnumerable<FluxService> services)
        {
            var finalFiles = new Dictionary<string, FluxTemplateFile>();

            foreach (var service in services)
            {
                foreach (var template in templates)
                {
                    service.Environments.ForEach(environment =>
                        {

                            var key = template.Key.Replace(Constants.Flux.Templates.PROGRAMME_FOLDER, teamConfig.ProgrammeName)
                                .Replace(Constants.Flux.Templates.TEAM_KEY, teamConfig.TeamName)
                                .Replace(Constants.Flux.Templates.ENV_KEY, $"{environment.Name[..3]}/0{environment.Name[3..]}")
                                .Replace(Constants.Flux.Templates.SERVICE_KEY, service.Name);
                            key = $"services/{key}";

                            if (!finalFiles.Any(file => file.Key.Equals(key)))
                            {
                                var newFile = template.Value.DeepCopy();
                                var tokens = new List<FluxConfig>
                                {
                                    new() { Key = Constants.Flux.Templates.VERSION_TOKEN, Value = Constants.Flux.Templates.DEFAULT_VERSION_TOKEN_VALUE },
                                    new() { Key = Constants.Flux.Templates.VERSION_TAG_TOKEN, Value = Constants.Flux.Templates.DEFAULT_VERSION_TAG_TOKEN_VALUE },
                                    new() { Key = Constants.Flux.Templates.MIGRATION_VERSION_TOKEN, Value = Constants.Flux.Templates.DEFAULT_MIGRATION_VERSION_TOKEN_VALUE },
                                    new() { Key = Constants.Flux.Templates.MIGRATION_VERSION_TAG_TOKEN, Value = Constants.Flux.Templates.DEFAULT_MIGRATION_VERSION_TAG_TOKEN_VALUE },
                                    new() { Key = Constants.Flux.Templates.PS_EXEC_VERSION_TOKEN, Value = Constants.Flux.Templates.PS_EXEC_DEFAULT_VERSION_TOKEN_VALUE }
                                };
                                tokens.ForEach(newFile.Content.ReplaceToken);

                                tokens =
                                [
                                    new() { Key = Constants.Flux.Templates.ENVIRONMENT_TOKEN, Value = environment.Name[..3]},
                                    new() { Key = Constants.Flux.Templates.ENV_INSTANCE_TOKEN, Value = environment.Name[3..]},
                                ];

                                var tenantConfigVariables = tenantConfig.Environments.First(x => x.Name.Equals(environment.Name)).ConfigVariables ?? [];

                                tokens.Union(environment.ConfigVariables).Union(tenantConfigVariables).ForEach(newFile.Content.ReplaceToken);
                                finalFiles.Add(key, newFile);
                            }
                        });
                }
            }
            return finalFiles;
        }

        private static FluxTemplateFile UpdateServiceKustomizationFiles(FluxTemplateFile serviceTemplate, string templateKey, FluxService service)
        {
            if (templateKey.Equals(Constants.Flux.Templates.TEAM_SERVICE_KUSTOMIZATION_FILE) && service.HasDatastore())
            {
                var content = serviceTemplate.Content[Constants.Flux.Templates.RESOURCES_KEY];
                AddItemToList(content, "pre-deploy-kustomize.yaml");
            }
            if (templateKey.Equals(Constants.Flux.Templates.TEAM_SERVICE_KUSTOMIZATION_FILE) && service.Type == FluxServiceType.HelmOnly)
            {
                var content = serviceTemplate.Content[Constants.Flux.Templates.RESOURCES_KEY];
                RemoveItemFromList(content, "infra-kustomize.yaml");
            }
            if (templateKey.Equals(Constants.Flux.Templates.DEPLOY_KUSTOMIZE_FILE) && service.Type == FluxServiceType.HelmOnly)
            {
                var content = serviceTemplate.Content[Constants.Flux.Templates.SPEC_KEY];
                RemoveItemFromDictionary(content, Constants.Flux.Templates.DEPENDS_ON_KEY);

                var substituteFrom = new YamlQuery(content)
                            .On(Constants.Flux.Templates.POST_BUILD_KEY)
                            .Get(Constants.Flux.Templates.SUBSTITUTE_FROM_KEY)
                            .ToList<List<object>>().FirstOrDefault();
                if (substituteFrom != null)
                {
                    var miCrendential = substituteFrom.First(x => ((Dictionary<object, object>)x)[Constants.Flux.Templates.NAME_KEY].Equals(Constants.Flux.Templates.SUBSTITUTE_SERVICE_MI_CREDENTIAL_KEY));
                    RemoveItemFromList(substituteFrom, miCrendential);
                }
            }
            return serviceTemplate;
        }

        private static void UpdateServicePatchFiles(Dictionary<string, FluxTemplateFile> serviceFiles, FluxService service, FluxTeamConfig teamConfig)
        {
            foreach (var file in serviceFiles)
            {
                service.Environments.ForEach(env =>
                {
                    var filePattern = string.Format(Constants.Flux.Services.TEAM_SERVICE_DEPLOY_ENV_PATCH_FILE, teamConfig.ProgrammeName, teamConfig.TeamName, service.Name, $"{env.Name[..3]}/0{env.Name[3..]}");
                    if (service.Type.Equals(FluxServiceType.Backend) && file.Key.Equals(filePattern))
                    {
                        new YamlQuery(file.Value.Content)
                            .On(Constants.Flux.Templates.SPEC_KEY)
                            .On(Constants.Flux.Templates.VALUES_KEY)
                            .Remove(Constants.Flux.Templates.LABELS_KEY)
                            .Remove(Constants.Flux.Templates.INGRESS_KEY);
                    }
                    filePattern = string.Format(Constants.Flux.Services.TEAM_SERVICE_INFRA_ENV_PATCH_FILE, teamConfig.ProgrammeName, teamConfig.TeamName, service.Name, $"{env.Name[..3]}/0{env.Name[3..]}");
                    if (service.Type.Equals(FluxServiceType.Frontend) && file.Key.Equals(filePattern))
                    {
                        new YamlQuery(file.Value.Content)
                            .On(Constants.Flux.Templates.SPEC_KEY)
                            .On(Constants.Flux.Templates.VALUES_KEY)
                            .Remove(Constants.Flux.Templates.POSTGRESRESOURCEGROUPNAME_KEY)
                            .Remove(Constants.Flux.Templates.POSTGRESSERVERNAME_KEY);
                    }
                });
            }
        }

        private static void CreateTeamVariables(FluxTeamConfig teamConfig)
        {
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.Templates.PROGRAMME_NAME_TOKEN, Value = teamConfig.ProgrammeName });
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.Templates.TEAM_NAME_TOKEN, Value = teamConfig.TeamName });
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.Templates.SERVICE_CODE_TOKEN, Value = teamConfig.ServiceCode });
        }

        private static void FilterEnvironmentsByName(List<FluxService> services, string environment)
        {
            foreach (var service in services)
            {
                service.Environments = service.Environments.Where(env => env.Name.Equals(environment)).ToList();
            }
        }

        private async Task PushFilesToFluxRepository(GitRepo gitRepoFluxServices, string teamName, string? serviceName, string? environment, Dictionary<string, FluxTemplateFile> generatedFiles)
        {
            var branchName = $"refs/heads/{gitRepoFluxServices.Reference}";

            string message = (string.IsNullOrEmpty(serviceName) ? teamName.ToLower() : serviceName.ToLower()) +
                             (string.IsNullOrEmpty(environment) ? "" : $" {environment.ToLower()}") +
                             " manifest";

            logger.LogInformation("Creating commit for the branch:'{BranchName}'.", branchName);
            var commitRef = await gitHubRepository.CreateCommitAsync(gitRepoFluxServices, generatedFiles, message, branchName);

            if (commitRef != null)
            {
                logger.LogInformation("Updating branch:'{BranchName}' with the changes.", branchName);
                await gitHubRepository.UpdateBranchAsync(gitRepoFluxServices, branchName, commitRef.Sha);
            }
            else
            {
                logger.LogInformation("No changes found in the flux files for the team:'{TeamName}' or the service:{ServiceName}.", teamName, serviceName);
            }
        }

        private static void AddItemToList(object content, string item)
        {
            if (content is not List<object> list)
            {
                throw new InvalidOperationException($"Unexpected type: {content.GetType()}");
            }

            if (!list.Exists(x => x.Equals(item)))
            {
                list.Add(item);
            }
        }

        private static void RemoveItemFromList<T>(object content, T item)
        {
            if (content is not List<object> list)
            {
                throw new InvalidOperationException($"Unexpected type: {content.GetType()}");
            }
            list.Remove(item ?? throw new ArgumentNullException(nameof(item)));
        }

        private static void RemoveItemFromDictionary(object content, string item)
        {
            if (content is not Dictionary<object, object> dictionary)
            {
                throw new InvalidOperationException($"Unexpected type: {content.GetType()}");
            }
            dictionary.Remove(item);
        }

        private async Task MergeEnvServicesManifestsAsync(string programmeName, string teamName, IEnumerable<FluxService> services, Dictionary<string, FluxTemplateFile> generatedFiles)
        {
            foreach (var envName in services.SelectMany(services => services.Environments.Select(env => env.Name)).Distinct())
            {
                var fileName = string.Format(Constants.Flux.Services.TEAM_SERVICE_ENV_KUSTOMIZATION_FILE, programmeName, teamName, $"{envName[..3]}/0{envName[3..]}");
                if (generatedFiles.TryGetValue(fileName, out var file))
                {
                    var config = await gitHubRepository.GetFileContentAsync<Dictionary<object, object>>(fluxServiceRepo, fileName);
                    foreach (var serviceName in services.Where(service => service.Environments.Exists(env => env.Name == envName)).Select(service => service.Name))
                    {
                        var item = new YamlQuery(config ?? file.Content)
                            .On(Constants.Flux.Templates.RESOURCES_KEY)
                            .Get().ToList<List<object>>();
                        AddItemToList(item[0], $"../../{serviceName}");
                        generatedFiles[fileName] = new FluxTemplateFile(config ?? file.Content);
                    }
                }
            }
        }

        private async Task MergeEnvTeamsManifestsAsync(string programmeName, string teamName, IEnumerable<FluxService> services, Dictionary<string, FluxTemplateFile> generatedFiles)
        {
            foreach (var envName in services.SelectMany(service => service.Environments.Select(env => env.Name[..3])).Distinct())
            {
                var fileName = string.Format(Constants.Flux.Services.TEAM_ENV_BASE_KUSTOMIZATION_FILE, envName);
                var config = await gitHubRepository.GetFileContentAsync<Dictionary<object, object>>(fluxServiceRepo, fileName);

                if (config != null)
                {
                    var item = new YamlQuery(config)
                            .On(Constants.Flux.Templates.RESOURCES_KEY)
                            .Get().ToList<List<object>>();
                    AddItemToList(item[0], $"../../../{programmeName}/{teamName}/base/patch");
                    generatedFiles[fileName] = new FluxTemplateFile(config);
                }
            }
        }

        private static List<KeyValuePair<string, FluxTemplateFile>> UpdateHelmDeployDirectoryNames(IEnumerable<KeyValuePair<string, FluxTemplateFile>> templates)
        {
            var transformedTemplates = new List<KeyValuePair<string, FluxTemplateFile>>();
            foreach (var template in templates)
            {
                if (template.Key.StartsWith(Constants.Flux.Templates.SERVICE_HELMONLY_DEPLOY_FOLDER))
                {
                    transformedTemplates.Add(new KeyValuePair<string, FluxTemplateFile>(template.Key.Replace(Constants.Flux.Templates.SERVICE_HELMONLY_DEPLOY_FOLDER, Constants.Flux.Templates.SERVICE_DEPLOY_FOLDER), template.Value));
                }
                else
                {
                    transformedTemplates.Add(new KeyValuePair<string, FluxTemplateFile>(template.Key, template.Value));
                }

            }
            return transformedTemplates;
        }
        #endregion
    }
}
