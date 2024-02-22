using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADP.Portal.Core.Azure.Infrastructure
{
    public interface IAzureAADGroupService
    {
        public Task<bool> AddToAADGroupAsync(Guid groupId, string userPrincipalName);
    }
}
