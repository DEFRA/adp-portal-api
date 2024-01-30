using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ADP.Portal.Api.Tests
{
    [TestFixture]
    public class GlobalExceptionHandlerTests
    {
        private readonly Mock<ILogger<GlobalExceptionHandler>> loggerMock;
        private readonly GlobalExceptionHandler handler;
        public GlobalExceptionHandlerTests()
        {
            loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
            handler = new GlobalExceptionHandler(loggerMock.Object);
        }

        [Test]
        public async Task TryHandleAsync_LogsErrorAndWritesProblemDetails()
        {
            // Arrange
            var exception = new Exception("Test exception");
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            // Act
            var result = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    exception,
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(httpContext.Response.Body).ReadToEnd();
            Assert.That(responseBody, Is.EquivalentTo("{\"title\":\"Server error\",\"status\":500}"));
        }
    }
}
