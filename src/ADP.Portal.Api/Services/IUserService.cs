using Microsoft.AspNetCore.Mvc;

namespace ADP.Portal.Api.Services
{
    public interface IUserService
    {
        void AddUser([FromBody] string userPrincipalName);
    }
}
