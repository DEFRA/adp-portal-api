using ADP.Portal.Api.Config;
using ADP.Portal.Api.Models;
using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Services;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ADP.Portal.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdoProjectController : ControllerBase
    {
        private readonly ILogger<AdoProjectController> logger;
        private readonly IOptions<AdpAdoProjectConfig> adpAdpProjectConfig;
        private readonly IAdoProjectService adoProjectService;

        public AdoProjectController(ILogger<AdoProjectController> logger, IOptions<AdpAdoProjectConfig> adpAdpProjectConfig, IAdoProjectService adoProjectService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.adpAdpProjectConfig = adpAdpProjectConfig ?? throw new ArgumentNullException(nameof(adpAdpProjectConfig));
            this.adoProjectService = adoProjectService ?? throw new ArgumentNullException(nameof(adoProjectService));
        }

        [HttpGet("{projectName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetAdoProject(string projectName)
        {
            logger.LogInformation($"Getting project {projectName}");
            var project = await adoProjectService.GetProjectAsync(projectName);
            if (project == null)
            {
                logger.LogWarning($"Project {projectName} not found");
                return NotFound();
            }
            return Ok(project);
        }

        [HttpPatch("{projectName}/onboard")]
        public async Task<ActionResult> OnBoardAsync(string projectName, [FromBody] OnBoardAdoProjectRequest onBoardRequest)
        {
            var project = await adoProjectService.GetProjectAsync(projectName);
            if (project == null)
            {
                logger.LogWarning($"Project {projectName} not found");
                return NotFound();
            }

            var adoProject = onBoardRequest.Adapt<AdoProject>();
            adoProject.ProjectReference = project;

            await adoProjectService.OnBoardAsync(adpAdpProjectConfig.Value.Name, adoProject);

            return NoContent();
        }

    }
}