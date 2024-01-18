﻿using System.ComponentModel.DataAnnotations;

namespace ADP.Portal.Api.Models
{
    public class OnBoardAdoProjectRequest
    {
        public List<AdoEnvironment> Environments { get; set; }
        
        public List<string> ServiceConnections { get; set; }
        
        public List<string> AgentPools { get; set; }

        public List<AdoVariableGroup>? VariableGroups { get; set; }
    }
}
