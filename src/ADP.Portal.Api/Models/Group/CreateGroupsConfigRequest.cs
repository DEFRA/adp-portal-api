using System.ComponentModel.DataAnnotations;

namespace ADP.Portal.Api.Models.Group
{
    public sealed class CreateGroupsConfigRequest
    {
        [Required]public required List<string> Members { get; set; }
    }
}
