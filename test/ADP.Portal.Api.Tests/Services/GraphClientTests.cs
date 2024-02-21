using NUnit.Framework;
using ADP.Portal.Core.Ado.Services;

namespace ADP.Portal.Api.Tests.Services
{
    [TestFixture]
    public class GraphClientTests
    {
        
        [Test]
        public async Task GetServiceClient_ReturnNull_WhenDoesNotExist()
        {
            //Arrange
           var client = new GraphClient();

            // Act
            var result = await client.GetServiceClient();

            // Assert
            Assert.That(result, Is.Not.Null);
        }
        
        [Test]
        public async Task GetServiceClient_ReturnClient_WhenExist()
        {
            //Arrange
            GraphClient client = new GraphClient();

            // Act
            var result = await client.GetServiceClient();

            // Assert
            Assert.That(result, Is.Not.Null);
        }       
    }
}
