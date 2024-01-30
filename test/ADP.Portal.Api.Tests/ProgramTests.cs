using ADP.Portal.Api.Config;
using ADP.Portal.Api.Mapster;
using ADP.Portal.Api.Wrappers;
using ADP.Portal.Core.Ado.Infrastructure;
using ADP.Portal.Core.Ado.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADP.Portal.Api.Tests
{
    [TestFixture]
    public class ProgramTests
    {
        private readonly WebApplicationBuilder builder;

        public ProgramTests()
        {
            builder =  WebApplication.CreateBuilder();
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
    }
}
