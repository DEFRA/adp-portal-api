using System.ComponentModel.DataAnnotations;

namespace ADP.Portal.Api.Models.Ado
{
    public sealed class OnBoardAdoProjectRequest
    {
        [Required]public required List<AdoEnvironment> Environments { get; set; }

        [Required]public required List<string> ServiceConnections { get; set; }

        [Required]public required List<string> AgentPools { get; set; }

        public List<AdoVariableGroup>? VariableGroups { get; set; }
    }
}
