using System.ComponentModel.DataAnnotations;

namespace ADP.Portal.Api.Models.Flux
{
    public sealed class ServiceConfigRequest
    {
        [Required] public required string Name { get; set; }
        [Required] public required bool IsFrontend { get; set; }
        public bool IsHelmOnly { get; set; } = false;
        public List<string>? Environments { get; set; }
        public Dictionary<string, string>? ConfigVariables { get; set; }
    }
}
