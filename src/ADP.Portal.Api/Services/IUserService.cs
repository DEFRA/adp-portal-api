using Microsoft.AspNetCore.Mvc;

namespace ADP.Portal.Api.Services
{
    public interface IUserService
    {
        void AddOpenVPNUser([FromBody] string userPrincipalName);
    }
}
