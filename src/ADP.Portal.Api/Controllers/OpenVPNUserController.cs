﻿using Microsoft.AspNetCore.Mvc;
using ADP.Portal.Api.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ADP.Portal.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OpenVPNUserController : ControllerBase
    {
        private readonly IUserService _UserService;
        public OpenVPNUserController(IUserService userService, ILogger<OpenVPNUserController> logger)
        {
            _UserService = userService;
        }        

        // POST api/<UserController>
        [HttpPost()]
        public void AddOpenVPNUser(string userPrincipalName)
        {
            _UserService.AddOpenVPNUser(userPrincipalName);
        }             
    }
}
