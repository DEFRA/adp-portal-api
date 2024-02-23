using ADP.Portal.Core.Azure.Infrastructure;
using ADP.Portal.Core.Azure.Services;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using NSubstitute;
using NUnit.Framework;
using System.Reflection;


namespace ADP.Portal.Core.Tests.Azure.Infrastructure
{
    [TestFixture]
    public class AzureAADGroupServiceTests
    {
        private readonly IUserGroupService serviceMock;
        
        private readonly GraphServiceClient graphServiceClientMock;
    }
}
