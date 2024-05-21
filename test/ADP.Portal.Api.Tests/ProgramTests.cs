﻿using ADP.Portal.Api.Wrappers;
using ADP.Portal.Core.Ado.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using NUnit.Framework;
using YamlDotNet.Serialization;
using NSubstitute;
using ADP.Portal.Api.Config;

namespace ADP.Portal.Api.Tests
{
    public static class AppBuilder
    {
        public static WebApplicationBuilder Create()
        {
            IEnumerable<KeyValuePair<string, string?>> appInsightConfigList = [new KeyValuePair<string, string?>("AppInsights:ConnectionString", "InstrumentationKey=" + Guid.NewGuid().ToString())];
            var appInsightConfig = new ConfigurationBuilder()
                            .AddInMemoryCollection(appInsightConfigList)
                            .Build();
            var builder = WebApplication.CreateBuilder();
            builder.Configuration.AddConfiguration(appInsightConfig);
            Program.ConfigureApp(builder);
            return builder;
        }
    }

    [TestFixture]
    public class ProgramTests
    {

        [Test]
        public void TestConfigureApp()
        {
            // Arrange                       
            var builder = AppBuilder.Create();

            // Act
            var result = builder.Build();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void TestAzureCredentialResolution()
        {
            // Arrange
            var builder = AppBuilder.Create();

            // Act
            var app = builder.Build();
            var result = app.Services.GetService<IAzureCredential>();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void TestVssConnectionResolution()
        {
            // Arrange
            var builder = AppBuilder.Create();
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
            var result = app.Services.GetService<Task<IVssConnection>>();

            // Assert
            Assert.That(result, Is.Not.Null);
        }


        [Test]
        public void TestGraphServiceClientResolution()
        {
            // Arrange
            var builder = AppBuilder.Create();
            KeyValuePair<string, string?>[] aadConfig =
                [
                   new KeyValuePair<string, string?>("AzureAd:TenantId", Guid.NewGuid().ToString()),
                   new KeyValuePair<string, string?>("AzureAd:SpClientId", Guid.NewGuid().ToString()),
                   new KeyValuePair<string, string?>("AzureAd:SpClientSecret", Guid.NewGuid().ToString())
                ];

            IEnumerable<KeyValuePair<string, string?>> aadConfigList = aadConfig;
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(aadConfigList)
                .Build();        
            builder.Configuration.AddConfiguration(configuration);
            Program.ConfigureApp(builder);


            // Act
            var app = builder.Build();
            var result = app.Services.GetService<GraphServiceClient>();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void TestApiVersioningConfiguration()
        {
            // Arrange
            var builder = AppBuilder.Create();

            // Act
            var app = builder.Build();
            app.MapControllers();
            var result = app.Services.GetService<IAzureCredential>();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void TestSerializerResolution()
        {
            // Arrange
            var data = new Dictionary<object, object>
            {
                { "IsValid", true },
                { "Counter", 5 }
            };
            var builder = AppBuilder.Create();

            // Act
            var app = builder.Build();
            app.MapControllers();
            var serializer = app.Services.GetService<ISerializer>();
            var result = serializer?.Serialize(data);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void TestDeserializerResolution()
        {
            // Arrange
            var data = new StringReader(@"
                    isValid: 'true'
                    counter: 5
                ");
            var builder = AppBuilder.Create();

            // Act
            var app = builder.Build();
            app.MapControllers();
            var deserializer = app.Services.GetService<IDeserializer>();
            var result = deserializer?.Deserialize(data);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.GetType(), Is.EqualTo(typeof(Dictionary<object, object>)));
        }

        [Test]
        public void TestOpenTelemetry()
        {
            // Arrange
            var builder = AppBuilder.Create();
            KeyValuePair<string, string?>[] appEnvConfig =
                [
                   new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "Production"),
                   new KeyValuePair<string, string?>("UserAssignedIdentityResourceId", Guid.NewGuid().ToString()),
                ];
            IEnumerable<KeyValuePair<string, string?>> appEnvConfigList = appEnvConfig;            
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(appEnvConfigList)
                .Build();
            builder.Configuration.AddConfiguration(configuration);
            Program.ConfigureApp(builder);


            // Act
            var app = builder.Build();

            // Assert
            Assert.That(app, Is.Not.Null);
        }

        [Test]
        public void TestGitHub()
        {
            // Arrange
            var builder = AppBuilder.Create();
            KeyValuePair<string, string?>[] appEnvConfig =
                [
                   new KeyValuePair<string, string?>("GitHubAppAuth:Owner", "defra"),
                   new KeyValuePair<string, string?>("GitHubAppAuth:AppName", "test"),
                   new KeyValuePair<string, string?>("GitHubAppAuth:AppId", "test"),
                   new KeyValuePair<string, string?>("GitHubAppAuth:PrivateKeyBase64", Guid.NewGuid().ToString()),
                ];
            IEnumerable<KeyValuePair<string, string?>> appEnvConfigList = appEnvConfig;
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(appEnvConfigList)
                .Build();
            builder.Services.Configure<GitHubAppAuthConfig>(configuration);
            Program.ConfigureApp(builder);


            // Act
            var app = builder.Build();

            // Assert
            Assert.That(app, Is.Not.Null);
        }

    }
}
