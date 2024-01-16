using ADP.Portal.Api.Models;
using ADP.Portal.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ADP.Portal.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdoProjectController: ControllerBase
    {
        private readonly ILogger<AdoProjectController> logger;
        private readonly IAdoProjectService adoProjectService;

        public AdoProjectController(ILogger<AdoProjectController> logger, IAdoProjectService adoProjectService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger)); 
            this.adoProjectService = adoProjectService ?? throw new ArgumentNullException(nameof(adoProjectService));
        }

        [HttpGet("{projectName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AdoProject>> GetAdoProject(string projectName)
        {
            logger.LogInformation($"Getting project {projectName}");
            var project = await adoProjectService.GetProjectAsync(projectName);
            if (project == Guid.Empty)
            {
                logger.LogWarning($"Project {projectName} not found");
                return NotFound();
            }
            return Ok(project);
        }

        [HttpPut("{projectName}/onboard")]
        public async Task<ActionResult> OnBoardAsync(string projectName)
        {
            var project = await adoProjectService.GetProjectAsync(projectName);
            if (project == Guid.Empty)
            {
                logger.LogWarning($"Project {projectName} not found");
                return NotFound();
            }
            return Ok(project);
        }

    }
}
