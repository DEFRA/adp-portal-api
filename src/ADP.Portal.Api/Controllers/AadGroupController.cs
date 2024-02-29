using ADP.Portal.Api.Config;
using ADP.Portal.Api.Models;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ADP.Portal.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AadGroupController : ControllerBase
    {
        private readonly IGitOpsConfigService gitOpsConfigService;
        public readonly IOptions<AzureAdConfig> azureAdConfig;
        private readonly IOptions<AdpTeamGitRepoConfig> adpTeamGitRepoConfig;

        public AadGroupController(IGitOpsConfigService gitOpsConfigService,
            IOptions<AzureAdConfig> azureAdConfig , IOptions<AdpTeamGitRepoConfig> adpTeamGitRepoConfig)
        {
            this.gitOpsConfigService = gitOpsConfigService;
            this.azureAdConfig = azureAdConfig;
            this.adpTeamGitRepoConfig = adpTeamGitRepoConfig;
        }

        [HttpPut("sync/{teamName}/{syncConfigtype}")]
        public async Task<ActionResult> SyncGroupsAsync(string teamName, string syncConfigtype)
        {
            if (!Enum.TryParse<SyncConfigType>(syncConfigtype, true, out var syncConfigtypeEnum))
            {
                return BadRequest("Invalid sync config type.");
            }

            var configType = (ConfigType)syncConfigtypeEnum;
            var teamRepo = adpTeamGitRepoConfig.Value.Adapt<GitRepo>();

            var isConfigExists = await gitOpsConfigService.IsConfigExistsAsync(teamName, configType, teamRepo);
            if (!isConfigExists)
            {
                return BadRequest($"Team '{teamName}' config not found."); 
            }

            var ownerId = azureAdConfig.Value.SpObjectId;

            var result = await gitOpsConfigService.SyncGroupsAsync(teamName, ownerId, configType, teamRepo);

            if(result.Error.Count> 0)
            {
                return Ok(result.Error);
            }

            return NoContent();
        }
    }
}
