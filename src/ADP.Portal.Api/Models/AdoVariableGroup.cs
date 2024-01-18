using System.ComponentModel.DataAnnotations;

namespace ADP.Portal.Api.Models
{
    public class AdoVariableGroup
    {
        
        public string Name { get; set; }

        public string? Description { get; set; }

        public List<AdoVariable> Variables { get; set; }
    }
}
