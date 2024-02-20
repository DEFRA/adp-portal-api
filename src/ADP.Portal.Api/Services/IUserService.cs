using Microsoft.AspNetCore.Mvc;

namespace ADP.Portal.Api.Services
{
    public interface IUserService
    {
        Task AddOpenVPNUser([FromBody] string userPrincipalName);
    }
}
