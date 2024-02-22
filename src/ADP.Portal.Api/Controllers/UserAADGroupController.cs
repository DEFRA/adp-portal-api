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
        private readonly IUserGroupService userGroupService;
        public readonly IOptions<AADGroupConfig> aadGroupConfig;

        public UserAADGroupController(IUserGroupService userGroupService, IOptions<AADGroupConfig> aadGroupConfig)
        {
            this.userGroupService = userGroupService;
            this.aadGroupConfig = aadGroupConfig;
        }

        [HttpPost("openvpn/add/{userPrincipalName}")]
        public async Task<ActionResult> AddUserToOpenVpnGroup(string userPrincipalName)
        {
            var openVpnGroupId = aadGroupConfig.Value.OpenVPNGroupId;

            await userGroupService.AddUserToGroupAsync(openVpnGroupId, userPrincipalName);
            return NoContent();
        }
    }
}
