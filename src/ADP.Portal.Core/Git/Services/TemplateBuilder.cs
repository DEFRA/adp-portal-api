using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Extensions;
using ADP.Portal.Core.Helpers;
using Microsoft.VisualStudio.Services.Common;

namespace ADP.Portal.Core.Git.Services
{
    internal class TemplateBuilder
    {
        public static Dictionary<string, Dictionary<object, object>> ProcessTemplates(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> files,
            FluxTenant tenantConfig, FluxTeamConfig fluxTeamConfig, string? serviceName = null)
        {
            var finalFiles = new Dictionary<string, Dictionary<object, object>>();

            var services = serviceName != null ? fluxTeamConfig.Services.Where(x => x.Name.Equals(serviceName)) : fluxTeamConfig.Services;
            if (services.Any())
            {
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
            templates.Where(x => !x.Key.StartsWith(FluxConstants.SERVICE_FOLDER) &&
                             !x.Key.StartsWith(FluxConstants.TEAM_ENV_FOLDER)).ForEach(file =>
                             {
                                 var key = file.Key.Replace(FluxConstants.PROGRAMME_FOLDER, teamConfig.ProgrammeName).Replace(FluxConstants.TEAM_KEY, teamConfig.TeamName);
                                 finalFiles.Add($"services/{key}", file.Value);
                             });

            // Create team environments
            var envTemplates = templates.Where(x => x.Key.Contains(FluxConstants.TEAM_ENV_FOLDER));
            finalFiles.AddRange(CreateEnvironmentFiles(envTemplates, tenantConfig, teamConfig, teamConfig.Services));

            // Create files for each service
            var serviceTemplates = templates.Where(x => x.Key.StartsWith(FluxConstants.SERVICE_FOLDER)).ToList();
            foreach (var service in services)
            {
                var serviceFiles = new Dictionary<string, Dictionary<object, object>>();
                var serviceTypeBasedTemplates = ServiceTypeBasedFiles(serviceTemplates, service);

                foreach (var template in serviceTypeBasedTemplates)
                {
                    if (!template.Key.Contains(FluxConstants.ENV_KEY))
                    {
                        var serviceTemplate = template.Value.DeepCopy();
                        if (template.Key.Equals(FluxConstants.TEAM_SERVICE_KUSTOMIZATION_FILE) && service.HasDatastore())
                        {
                            ((List<object>)serviceTemplate[FluxConstants.RESOURCES_KEY]).Add("pre-deploy-kustomize.yaml");
                        }
                        var key = template.Key.Replace(FluxConstants.PROGRAMME_FOLDER, teamConfig.ProgrammeName).Replace(FluxConstants.TEAM_KEY, teamConfig.TeamName).Replace(FluxConstants.SERVICE_KEY, service.Name);
                        serviceFiles.Add($"services/{key}", serviceTemplate);
                    }
                    else
                    {
                        serviceFiles.AddRange(CreateEnvironmentFiles([template], tenantConfig, teamConfig, [service]));
                    }
                }
                UpdateServicePatchFiles(serviceFiles, service, teamConfig);

                service.ConfigVariables.Add(new FluxConfig { Key = FluxConstants.TEMPLATE_VAR_DEPENDS_ON, Value = service.HasDatastore() ? FluxConstants.PREDEPLOY_KEY : FluxConstants.INFRA_KEY });
                service.ConfigVariables.Add(new FluxConfig { Key = FluxConstants.TEMPLATE_VAR_SERVICE_NAME, Value = service.Name });
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
                    matched = !filter.Key.StartsWith(FluxConstants.SERVICE_PRE_DEPLOY_FOLDER) && !filter.Key.StartsWith(FluxConstants.PRE_DEPLOY_KUSTOMIZE_FILE);
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
                            var key = template.Key.Replace(FluxConstants.PROGRAMME_FOLDER, teamConfig.ProgrammeName)
                                .Replace(FluxConstants.TEAM_KEY, teamConfig.TeamName)
                                .Replace(FluxConstants.ENV_KEY, $"{environment.Name[..3]}/0{environment.Name[3..]}")
                                .Replace(FluxConstants.SERVICE_KEY, service.Name);
                            key = $"services/{key}";

                            if (template.Key.Equals(FluxConstants.TEAM_ENV_KUSTOMIZATION_FILE, StringComparison.InvariantCultureIgnoreCase) &&
                                finalFiles.TryGetValue(key, out var existingEnv))
                            {
                                ((List<object>)existingEnv[FluxConstants.RESOURCES_KEY]).Add($"../../{service.Name}");
                            }
                            else
                            {
                                var newFile = template.Value.DeepCopy();
                                if (template.Key.Equals(FluxConstants.TEAM_ENV_KUSTOMIZATION_FILE, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    ((List<object>)newFile[FluxConstants.RESOURCES_KEY]).Add($"../../{service.Name}");
                                }

                                var tokens = new List<FluxConfig>
                            {
                                new() { Key = FluxConstants.TEMPLATE_VAR_VERSION, Value = FluxConstants.TEMPLATE_VAR_DEFAULT_VERSION },
                                new() { Key = FluxConstants.TEMPLATE_VAR_VERSION_TAG, Value = FluxConstants.TEMPLATE_VAR_DEFAULT_VERSION_TAG },
                                new() { Key = FluxConstants.TEMPLATE_VAR_MIGRATION_VERSION, Value = FluxConstants.TEMPLATE_VAR_DEFAULT_MIGRATION_VERSION },
                                new() { Key = FluxConstants.TEMPLATE_VAR_MIGRATION_VERSION_TAG, Value = FluxConstants.TEMPLATE_VAR_DEFAULT_MIGRATION_VERSION_TAG },
                                new() { Key = FluxConstants.TEMPLATE_VAR_PS_EXEC_VERSION, Value = FluxConstants.TEMPLATE_VAR_PS_EXEC_DEFAULT_VERSION }
                            };
                                tokens.ForEach(newFile.ReplaceToken);

                                tokens =
                                [
                                    new() { Key = FluxConstants.TEMPLATE_VAR_ENVIRONMENT, Value = environment.Name[..3]},
                                new() { Key = FluxConstants.TEMPLATE_VAR_ENV_INSTANCE, Value = environment.Name[3..]},
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
                    var filePattern = string.Format(FluxConstants.TEAM_SERVICE_DEPLOY_ENV_PATCH_FILE, teamConfig.ProgrammeName, teamConfig.TeamName, service.Name, $"{env.Name[..3]}/0{env.Name[3..]}");
                    if (service.Type.Equals(FluxServiceType.Backend) && file.Key.Equals(filePattern))
                    {
                        new YamlQuery(file.Value)
                            .On(FluxConstants.SPEC_KEY)
                            .On(FluxConstants.VALUES_KEY)
                            .Remove(FluxConstants.LABELS_KEY)
                            .Remove(FluxConstants.INGRESS_KEY);
                    }
                    filePattern = string.Format(FluxConstants.TEAM_SERVICE_INFRA_ENV_PATCH_FILE, teamConfig.ProgrammeName, teamConfig.TeamName, service.Name, $"{env.Name[..3]}/0{env.Name[3..]}");
                    if (service.Type.Equals(FluxServiceType.Frontend) && file.Key.Equals(filePattern))
                    {
                        new YamlQuery(file.Value)
                            .On(FluxConstants.SPEC_KEY)
                            .On(FluxConstants.VALUES_KEY)
                            .Remove(FluxConstants.POSTGRESRESOURCEGROUPNAME_KEY)
                            .Remove(FluxConstants.POSTGRESSERVERNAME_KEY);
                    }
                });
            }
        }

        private static void CreateTeamVariables(FluxTeamConfig teamConfig)
        {
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = FluxConstants.TEMPLATE_VAR_PROGRAMME_NAME, Value = teamConfig.ProgrammeName ?? string.Empty });
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = FluxConstants.TEMPLATE_VAR_TEAM_NAME, Value = teamConfig.TeamName ?? string.Empty });
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = FluxConstants.TEMPLATE_VAR_SERVICE_CODE, Value = teamConfig.ServiceCode ?? string.Empty });
        }
    }
}
