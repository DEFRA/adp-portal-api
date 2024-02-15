using Microsoft.AspNetCore.Mvc;
using ADP.Portal.Api.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ADP.Portal.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddUserController : ControllerBase
    {
        private readonly IUserService _UserService;
        private readonly ILogger<AddUserController> _logger;

        public AddUserController(IUserService userService, ILogger<AddUserController> logger)
        {
            _UserService = userService;
            _logger = logger;
        }        

        // POST api/<UserController>
        [HttpPost]
        public async Task AddUser([FromBody] string userPrincipalName)
        {
            _UserService.AddUser(userPrincipalName);
        }             
    }
}
