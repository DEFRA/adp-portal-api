using System.ComponentModel.DataAnnotations;

namespace ADP.Portal.Api.Models
{
    public class AdoVariable
    {        
        public string Name { get; set; }

        public string Value { get; set; }

        public bool IsSecret { get; set; }
    }
}
