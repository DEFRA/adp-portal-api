using ADP.Portal.Api.Config;
using ADP.Portal.Api.Models;
using ADP.Portal.Core.Git.Services;
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

        public AadGroupController(IGitOpsConfigService gitOpsConfigService,
            IOptions<AzureAdConfig> azureAdConfig)
        {
            this.gitOpsConfigService = gitOpsConfigService;
            this.azureAdConfig = azureAdConfig;
        }

        [HttpPut("sync/{teamName}/{syncConfigtype}")]
        public async Task<ActionResult> SyncGroupsAsync(string teamName, string syncConfigtype)
        {
            if (!Enum.TryParse<SyncConfigType>(syncConfigtype, true, out var syncConfigtypeEnum))
            {
                return BadRequest("Invalid sync config type.");
            }

            var tenant = azureAdConfig.Value.DirectoryName;
            var configType = (ConfigType)syncConfigtypeEnum;

            var isConfigExists = gitOpsConfigService.IsConfigExists(teamName, configType, tenant);
            if (!isConfigExists)
            {
                return BadRequest($"Team '{teamName}' config not found."); 
            }

            var ownerId = azureAdConfig.Value.SpObjectId;

            var result = await gitOpsConfigService.SyncGroupsAsync(teamName, ownerId, configType, tenant);

            if(result.Error.Count> 0)
            {
                return Ok(result.Error);
            }

            return NoContent();
        }
    }
}
