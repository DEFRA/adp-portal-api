using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADP.Portal.Core.Interfaces
{
    public interface IAdoProjectService
    {
        public Task<Guid> GetProjectAsync(string projectName);
    }
}
