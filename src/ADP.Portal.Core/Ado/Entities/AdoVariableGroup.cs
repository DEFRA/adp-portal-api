using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADP.Portal.Core.Ado.Entities
{
    public class AdoVariableGroup
    {
        public string Name { get; set; }

        public List<AdoVariable> Variables { get; set; }
        public string Description { get; internal set; }
    }
}
