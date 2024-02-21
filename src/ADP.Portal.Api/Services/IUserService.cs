using Microsoft.AspNetCore.Mvc;

namespace ADP.Portal.Api.Services
{
    public interface IUserService
    {
        public Task<string?> AddOpenVPNUser([FromBody] string userPrincipalName);
    }
}
