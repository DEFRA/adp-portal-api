﻿using ADP.Portal.Api.Config;
using ADP.Portal.Api.Models;
using ADP.Portal.Core.Application;
using ADP.Portal.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ADP.Portal.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdoProjectController : ControllerBase
    {
        private readonly ILogger<AdoProjectController> _logger;
        private readonly IOptions<AdpAdoProjectConfig> _adpProjectConfig;
        private readonly IAdoProjectService _adoProjectService;

        public AdoProjectController(ILogger<AdoProjectController> logger, IOptions<AdpAdoProjectConfig> adpProjectConfig, IAdoProjectService adoProjectService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _adpProjectConfig = adpProjectConfig ?? throw new ArgumentNullException(nameof(adpProjectConfig));
            _adoProjectService = adoProjectService ?? throw new ArgumentNullException(nameof(adoProjectService));
        }

        [HttpGet("{projectName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Models.AdoProject>> GetAdoProject(string projectName)
        {
            _logger.LogInformation($"Getting project {projectName}");
            var project = await _adoProjectService.GetProjectAsync(projectName);
            if (project == null)
            {
                _logger.LogWarning($"Project {projectName} not found");
                return NotFound();
            }
            return Ok(project);
        }

        [HttpPut("{projectName}/onboard")]
        public async Task<ActionResult> OnBoardAsync(string projectName)
        {
            var project = await _adoProjectService.GetProjectAsync(projectName);
            if (project == null)
            {
                _logger.LogWarning($"Project {projectName} not found");
                return NotFound();
            }

            var config = _adpProjectConfig.Value;

            if (config == null)
            {
                _logger.LogError("ADP Project configuration not found");
                return BadRequest("ADP Project configuration not found");
            }

            await _adoProjectService.OnBoardAsync(project, new Core.Domain.AdoProject
            {
                Name = config.Name,
                ServiceConnections = config.ServiceConnections
            });

            return Ok(project);
        }

    }
}
