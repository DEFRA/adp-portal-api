using ADP.Portal.Core.Ado.Services;
using NSubstitute;
using NUnit.Framework;

namespace ADP.Portal.Api.Tests.Services
{
    [TestFixture]    
    public class UserServiceTests
    {
        private readonly IGraphClient clientMock;

        public UserServiceTests()
        {
            clientMock = Substitute.For<IGraphClient>();            
        }  

        [Test]
        public async Task AddOpenVPNUser_ReturnsNotNull_WhenUserDoesNotExist()
        {
            //Arrange
            string? userPrincipaName = "TestUser";
            UserService service = new UserService(clientMock);

            // Act
            string? result = await service.AddOpenVPNUser(userPrincipaName);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task AddOpenVPNUser_ReturnUserId_WhenUserExist()
        {
            //Arrange
            string userPrincipaName = "testuser";
            UserService service = new UserService(clientMock);

            // Act
            string? result = await service.AddOpenVPNUser(userPrincipaName);
 
            // Assert
            Assert.That(result, Is.Not.Empty);
        }
    }
}

