using ADP.Portal.Api.Wrappers;
using ADP.Portal.Core.Ado.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ADP.Portal.Api.Tests
{
    [TestFixture]
    public class ProgramTests
    {
        private readonly WebApplicationBuilder builder;

        public ProgramTests()
        {
            builder = WebApplication.CreateBuilder();
        }

        [Test]
        public void TestConfigureApp()
        {
            // Arrange
            Program.ConfigureApp(builder);

            // Act
            var app = builder.Build();

            // Assert
            Assert.That(app, Is.Not.Null);
        }

        [Test]
        public void TestAzureCredentialResolution()
        {
            // Arrange
            Program.ConfigureApp(builder);

            // Act
            var app = builder.Build();
            var azureCredential = app.Services.GetService<IAzureCredential>();

            // Assert
            Assert.That(azureCredential, Is.Not.Null);
        }

        [Test]
        public void TestVssConnectionResolution()
        {
            // Arrange
            KeyValuePair<string, string?>[] adoConfig =
                [
                   new KeyValuePair<string, string?>("Ado:UsePatToken", "true"),
                   new KeyValuePair<string, string?>("Ado:PatToken", "TestPatToken")
                ];

            IEnumerable<KeyValuePair<string, string?>> adoConfigList = adoConfig;
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(adoConfigList)
                .Build();

            builder.Configuration.AddConfiguration(configuration);
            Program.ConfigureApp(builder);


            // Act
            var app = builder.Build();
            var vssConnection = app.Services.GetService<Task<IVssConnection>>();

            // Assert
            Assert.That(vssConnection, Is.Not.Null);
        }
    }
}
