using ADP.Portal.Api.Config;
using ADP.Portal.Api.Controllers;
using ADP.Portal.Core.Git.Services;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using System.Reflection;

namespace ADP.Portal.Api.Tests.Controllers
{
    [TestFixture]
    public class AadGroupControllerTests
    {
        private readonly AadGroupController controller;
        private readonly IOptions<AzureAdConfig> azureAdConfigMock;
        private readonly IOptions<AdpTeamGitRepoConfig> adpTeamGitRepoConfigMock;
        private readonly IGitOpsConfigService gitOpsConfigServiceMock;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        }

        public AadGroupControllerTests()
        {
            azureAdConfigMock = Substitute.For<IOptions<AzureAdConfig>>();
            adpTeamGitRepoConfigMock = Substitute.For<IOptions<AdpTeamGitRepoConfig>>();
            gitOpsConfigServiceMock = Substitute.For<IGitOpsConfigService>();
            controller = new AadGroupController(gitOpsConfigServiceMock, azureAdConfigMock, adpTeamGitRepoConfigMock);
        }
    }
}
