using Microsoft.AspNetCore.Mvc;
using ADP.Portal.Core.Azure.Services;
using Microsoft.Extensions.Options;
using ADP.Portal.Api.Config;

namespace ADP.Portal.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAADGroupController : ControllerBase
    {
        private readonly ILogger<UserAADGroupController> logger;
        private readonly IUserGroupService userGroupService;
        public readonly IOptions<AADGroupConfig> aadGroupConfig;

        public UserAADGroupController(ILogger<UserAADGroupController> logger, IUserGroupService userGroupService, IOptions<AADGroupConfig> aadGroupConfig)
        {
            this.userGroupService = userGroupService;
            this.aadGroupConfig = aadGroupConfig;
            this.logger = logger;
        }

        [HttpPost("openvpn/add/{userPrincipalName}")]
        public async Task<ActionResult> AddUserToOpenVpnGroup(string userPrincipalName)
        {
            var openVpnGroupId = aadGroupConfig.Value.OpenVPNGroupId;

            var userId = await userGroupService.GetUserIdAsync(userPrincipalName);

            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("User:'{userPrincipalName}' not found", userPrincipalName);
                return NotFound($"User not found");
            }

            var result = await userGroupService.AddUserToGroupAsync(openVpnGroupId, userPrincipalName, userId);
            if(result == true)
            {
                return NoContent();
            }

            return BadRequest();
        }
    }
}
