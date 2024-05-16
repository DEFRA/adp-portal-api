using System.ComponentModel.DataAnnotations;

namespace ADP.Portal.Api.Models.Flux
{
    public sealed class ManifestConfigRequest
    {
        [Required]public required bool Generate { get; set; }
    }
}
