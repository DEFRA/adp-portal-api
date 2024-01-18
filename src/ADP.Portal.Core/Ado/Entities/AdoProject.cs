using Microsoft.TeamFoundation.Core.WebApi;

namespace ADP.Portal.Core.Ado.Entities
{

    public class AdoProject
    {
        public TeamProjectReference ProjectReference { get; set; }

        public List<string> ServiceConnections { get; set; }

        public List<string> AgentPools { get; set; }

        public List<AdoEnvironment> Environments { get; set; }

        public List<AdoVariableGroup>? VariableGroups { get; set; }
    }
}
