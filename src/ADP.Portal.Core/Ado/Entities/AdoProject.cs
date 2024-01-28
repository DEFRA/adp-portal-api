using Microsoft.TeamFoundation.Core.WebApi;

namespace ADP.Portal.Core.Ado.Entities
{

    public class AdoProject
    {
        public required TeamProjectReference ProjectReference { get; set; }

        public required List<string> ServiceConnections { get; set; }

        public required List<string> AgentPools { get; set; }

        public required List<AdoEnvironment> Environments { get; set; }

        public List<AdoVariableGroup>? VariableGroups { get; set; }
    }
}
