using System.ComponentModel.DataAnnotations;

namespace ADP.Portal.Api.Models.Flux
{
    public sealed class TeamConfigRequest
    {
        [Required] public required string ProgrammeName { get; set; }
        [Required] public required string ServiceCode { get; set; }
        [Required] public required string TeamName { get; set; }
        [Required] public required List<FluxService> Services { get; set; }
        public Dictionary<string, string> ConfigVariables { get; set; } = [];
    }
}
