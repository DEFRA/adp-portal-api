using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Infrastructure;
using ADP.Portal.Core.Ado.Services;
using ADP.Portal.Core.Azure.Services;
using AutoFixture;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System.Collections.Generic;

namespace ADP.Portal.Core.Tests.Ado.Services
{
    [TestFixture]
    public class UserGroupServiceTests
    {
        private readonly IUserGroupService serviceMock;
        public UserGroupServiceTests()
        {
            serviceMock = Substitute.For<IUserGroupService>();
        }

        [Test]
        public async Task GetProjectAsync_ReturnsProject_WhenProjectExists()
        {
            // Arrange
            var userPrincipalName = "TestProject";
           
            // Act
            

            // Assert
            //Assert.That(result, Is.EqualTo(project));
        }

        [Test]
        public async Task GetProjectAsync_ReturnsNull_WhenProjectDoesNotExist()
        {
            // Arrange
            var projectName = "TestProject";
            
           
            // Act
            

            // Assert
            //Assert.That(result, Is.Null);
        }
        
    }
}