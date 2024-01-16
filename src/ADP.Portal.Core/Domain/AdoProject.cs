using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ADP.Portal.Core.Domain
{

    public class AdoProject
    {
        public string Name { get; set; }

        public List<string> ServiceConnections { get; set; }
    }
}
