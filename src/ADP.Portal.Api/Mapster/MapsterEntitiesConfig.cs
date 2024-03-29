﻿using System.Reflection;
using ADP.Portal.Api.Models.Flux;
using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Azure.Entities;
using Mapster;
using Microsoft.Graph.Models;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace ADP.Portal.Api.Mapster
{
    public static class MapsterEntitiesConfig
    {
        public static void EntitiesConfigure(this IServiceCollection services)
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

            TypeAdapterConfig<AdoVariableGroup, VariableGroupParameters>.NewConfig()
                .Map(dest => dest.VariableGroupProjectReferences, src => new List<VariableGroupProjectReference>() { new() { Name = src.Name, Description = src.Description } })
                .Map(dest => dest.Variables, src => src.Variables.ToDictionary(v => v.Name, v => new VariableValue(v.Value, v.IsSecret)));

            TypeAdapterConfig<AadGroup, Group>.NewConfig()
                .Map(dest => dest.MailEnabled, src => false)
                .Map(dest => dest.SecurityEnabled, src => true)
                .Map(dest => dest.MailNickname, src => src.DisplayName)
                .Map(dest => dest.AdditionalData, src => new Dictionary<string, object>
                  {
                    {
                        "owners@odata.bind" , new List<string>
                        {
                            $"https://graph.microsoft.com/v1.0/servicePrincipals/{src.OwnerId}",
                        }
                    }
                  });

            TypeAdapterConfig<FluxService, Core.Git.Entities.FluxService>.NewConfig()
                .Map(dest => dest.Environments, opt => opt.Environments.Select(x => new Core.Git.Entities.FluxEnvironment { Name = x }))
                .Map(dest => dest.Type, opt => opt.IsFrontend ? Core.Git.Entities.FluxServiceType.Frontend : Core.Git.Entities.FluxServiceType.Backend);
        }
    }
}
