using System.ComponentModel.DataAnnotations;

namespace ADP.Portal.Api.Models
{
    public class AdoEnvironment
    {
        public string Name { get; set; }

        public string? Description { get; set; }
    }
}
