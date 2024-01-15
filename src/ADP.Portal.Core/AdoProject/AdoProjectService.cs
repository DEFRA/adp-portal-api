using ADP.Portal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADP.Portal.Core.AdoProject
{
    public class AdoProjectService : IAdoProjectService
    {
        public Task<Guid> GetProjectAsync(string projectName)
        {
            throw new NotImplementedException();
        }
    }
}
