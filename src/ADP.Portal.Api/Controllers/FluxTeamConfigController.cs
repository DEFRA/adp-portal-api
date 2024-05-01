using ADP.Portal.Api.Config;
using ADP.Portal.Api.Models.Flux;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using Asp.Versioning;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ADP.Portal.Api.Controllers;

[Route("api/[controller]")]
[ApiVersion("1.0")]
[ApiController]
public class FluxTeamConfigController : Controller
{
    private readonly IFluxTeamConfigService fluxTeamConfigService;
    private readonly ILogger<FluxTeamConfigController> logger;
    private readonly IOptions<AzureAdConfig> azureAdConfig;

    public FluxTeamConfigController(IFluxTeamConfigService fluxTeamConfigService, ILogger<FluxTeamConfigController> logger,
        IOptions<AzureAdConfig> azureAdConfig)
    {
        this.fluxTeamConfigService = fluxTeamConfigService;
        this.logger = logger;
        this.azureAdConfig = azureAdConfig;
    }

    /// <summary>
    /// Read the Flux Configuration file defined for a specific team from the GitOps repository.
    /// </summary>
    /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
    /// <returns></returns>
    [HttpGet("{teamName}", Name = "GetFluxConfigForTeam")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetConfigAsync(string teamName)
    {
        logger.LogInformation("Reading Flux Config for the Team:'{TeamName}'", teamName);
        var result = await fluxTeamConfigService.GetConfigAsync<FluxTeamConfig>(teamName: teamName);

        if (result != null)
        {
            return Ok(result);
        }

        return NotFound();
    }

    /// <summary>
    /// Create a new Flux configuration file in the GitOps repository for a specific team.
    /// </summary>
    /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
    /// <param name="fluxConfigRequest">Required: Details about the Services, Environments & ConfigVariables for the team</param>
    /// <returns></returns>
    [HttpPost("{teamName}", Name = "CreateFluxConfigForTeam")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateConfigAsync(string teamName, [FromBody] TeamFluxConfigRequest fluxConfigRequest)
    {
        var newTeamConfig = fluxConfigRequest.Adapt<FluxTeamConfig>();

        logger.LogInformation("Creating Flux Config for the Team:'{TeamName}'", teamName);
        var result = await fluxTeamConfigService.CreateConfigAsync(teamName, newTeamConfig);
        if (result.Errors.Count > 0)
        {
            logger.LogError("Error while creating Flux Config for the Team:'{TeamName}'", teamName);
            return BadRequest(result.Errors);
        }

        return Created();
    }

    /// <summary>
    /// Updates the information in the Flux configuration file defined for the team in the GitOps repository.
    /// </summary>
    /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
    /// <param name="fluxConfigRequest">Required: Details about the Services, Environments & ConfigVariables for the team</param>
    /// <returns></returns>
    [HttpPut("{teamName}", Name = "UpdateFluxConfigForTeam")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateConfigAsync(string teamName, [FromBody] TeamFluxConfigRequest fluxConfigRequest)
    {
        var newTeamConfig = fluxConfigRequest.Adapt<FluxTeamConfig>();

        logger.LogInformation("Updating Flux Config for the Team:'{TeamName}'", teamName);
        var result = await fluxTeamConfigService.UpdateConfigAsync(teamName, newTeamConfig);

        if (!result.IsConfigExists)
        {
            logger.LogWarning("Flux Config not found for the Team:'{TeamName}'", teamName);
            return BadRequest($"Flux config not found for the team:{teamName}");
        }
        if (result.Errors.Count > 0)
        {
            logger.LogError("Error while updating Flux config for the Team:'{TeamName}'", teamName);
            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    /// <summary>
    /// This operation will create a new service in the Flux Config.
    /// </summary>
    /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
    /// <param name="serviceFluxConfigRequest">The request object containing all the necessary information to create a new service in the Flux Config.</param>
    /// <returns></returns>
    [HttpPost("{teamName}/services", Name = "CreateServiceFluxConfigForTeam")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateServiceAsync(string teamName, [FromBody] ServiceFluxConfigRequest serviceFluxConfigRequest)
    {
        var newTeamService = serviceFluxConfigRequest.Adapt<Core.Git.Entities.FluxService>();

        logger.LogInformation("Creating Service in the Flux Config for the Team:'{TeamName}'", teamName);
        var result = await fluxTeamConfigService.AddServiceAsync(teamName, newTeamService);

        if (!result.IsConfigExists)
        {
            return BadRequest($"Flux config not found for the team:{teamName}");
        }
        if (result.Errors.Count > 0)
        {
            logger.LogError("Error while updating Flux config for the Team:'{TeamName}'", teamName);
            return BadRequest(result.Errors);
        }

        return Created();
    }

    /// <summary>
    /// Add or update a service environment in the Flux configuration file defined for the team in the GitOps repository.
    /// </summary>
    /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
    /// <param name="serviceName">Required: Name of the Service</param>
    /// <param name="environmentRequest">Required: Details about the Environment for the service</param>
    /// <returns></returns>
    [HttpPost("{teamName}/services/{serviceName}/environments", Name = "AddEnvironmentForTeamService")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> AddServiceEnvironmentAsync(string teamName, string serviceName, [FromBody] string environmentRequest)
    {
        var newEnvironment = new FluxEnvironment { Name = environmentRequest };

        logger.LogInformation("Adding Environment for the Service:'{ServiceName}' in the Team:'{TeamName}'", serviceName, teamName);
        var result = await fluxTeamConfigService.AddServiceEnvironmentAsync(teamName, serviceName, newEnvironment);

        if (!result.IsConfigExists)
        {
            return BadRequest(result.Errors[0]);
        }

        if (result.Errors.Count > 0)
        {
            logger.LogError("Error while adding Environment for the Service:'{ServiceName}' in the Team:'{TeamName}'", serviceName, teamName);
            return BadRequest(result.Errors);
        }

        return Created();
    }

    /// <summary>
    /// This operation will generate the Flux Manifests based on the configuration defined for the specific team in the GitOps repository.
    /// </summary>
    /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
    /// <param name="serviceName">Optional: Generate manifests only for this service if specified. Default is All services</param>
    /// <returns></returns>
    [HttpPost("{teamName}/generate", Name = "GenerateFluxConfigForTeam")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GenerateAsync(string teamName, [FromQuery] string? serviceName, [FromQuery] string? environment = null)
    {
        var tenantName = azureAdConfig.Value.TenantName;

        logger.LogInformation("Generating Flux Manifests for the Team:{TeamName}", teamName);
        var result = await fluxTeamConfigService.GenerateConfigAsync(tenantName, teamName, serviceName, environment);

        if (!result.IsConfigExists)
        {
            return BadRequest($"Flux generator config not found for the team:{teamName}");
        }
        if (result.Errors.Count > 0)
        {
            logger.LogError("Error while generating manifests for the Team:'{TeamName}'", teamName);
            return BadRequest(result.Errors);
        }

        return NoContent();
    }
}