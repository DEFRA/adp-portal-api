using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADP.Portal.Core.Ado.Entities
{
    public class AdoVariable
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsSecret { get; set; }
    }
}
