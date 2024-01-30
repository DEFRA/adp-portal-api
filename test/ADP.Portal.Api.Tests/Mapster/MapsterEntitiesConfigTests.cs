using ADP.Portal.Api.Mapster;
using ADP.Portal.Core.Ado.Entities;
using AutoFixture;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Moq;
using NUnit.Framework;

namespace ADP.Portal.Api.Tests.Mapster
{
    [TestFixture]
    public class MapsterEntitiesConfigTests
    {
        private readonly Mock<IServiceCollection> servicesMock;
        
        public MapsterEntitiesConfigTests()
        {
            servicesMock = new Mock<IServiceCollection>();
        }

        [Test]
        public void TestEntitiesConfigure()
        {
            // Arrange
            var fixture = new Fixture();
            var adoVariableGroup = fixture.Build<AdoVariableGroup>().Create();

            // Act
            servicesMock.Object.EntitiesConfigure();
            var results = adoVariableGroup.Adapt<VariableGroupParameters>();

            // Assert
            Assert.That(results,Is.Not.Null);
            Assert.That(results.VariableGroupProjectReferences, Is.Not.Null);
            Assert.That(results.Variables, Is.Not.Null);
        }
    }
}
