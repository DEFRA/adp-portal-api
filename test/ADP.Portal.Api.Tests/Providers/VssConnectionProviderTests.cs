using ADP.Portal.Api.Config;
using ADP.Portal.Api.Providers;
using ADP.Portal.Api.Wrappers;
using AutoFixture;
using Azure.Core;
using Azure.Identity;
using Moq;
using NUnit.Framework;

namespace ADP.Portal.Api.Tests.Providers
{
    [TestFixture]
    public class VssConnectionProviderTests
    {
        private readonly Mock<IAzureCredential> azureCredentialMock;
        private readonly string organizationUrl = "http://localhost";
        private readonly Mock<DefaultAzureCredential> defaultAzureCredentialMock;

        public VssConnectionProviderTests()
        {
            azureCredentialMock = new Mock<IAzureCredential>();
            defaultAzureCredentialMock = new Mock<DefaultAzureCredential>();
        }

        [Test]
        public async Task GetConnectionAsync_UsesPatToken_WhenUsePatTokenIsTrue()
        {
            //Arrange
            var fixture= new Fixture();
            var adoConfig = fixture.Build<AdoConfig>()
                .With(c => c.OrganizationUrl, organizationUrl).Create();
            var provider = new VssConnectionProvider(azureCredentialMock.Object, adoConfig);

            // Act
            var result = await provider.GetConnectionAsync();

            // Assert
            Assert.That(result,Is.Not.Null);
        }

        [Test]
        public async Task GetConnectionAsync_UsesAccessToken_WhenUsePatTokenIsFalse()
        {
            //Arrange
            var fixture = new Fixture();
            var adoConfig = fixture.Build<AdoConfig>()
                .With(c => c.OrganizationUrl, organizationUrl)
                .With(c=>c.UsePatToken, false)
                .Create();
            var accessToken = fixture.Build<AccessToken>().Create();
            azureCredentialMock.Setup(x => x.GetTokenAsync(It.IsAny<TokenRequestContext>())).ReturnsAsync(accessToken);

            var provider = new VssConnectionProvider(azureCredentialMock.Object,adoConfig);

            // Act
            var result = await provider.GetConnectionAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
        }
    }
}
