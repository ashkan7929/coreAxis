using CoreAxis.ApiGateway.HealthChecks;
using CoreAxis.EventBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Tests.ApiGateway
{
    /// <summary>
    /// Unit tests for the health check implementations.
    /// </summary>
    public class HealthCheckTests
    {
        /// <summary>
        /// Tests that the EventBusHealthCheck returns Healthy when the event bus is operational.
        /// </summary>
        [Fact]
        public async Task EventBusHealthCheck_WithOperationalEventBus_ShouldReturnHealthy()
        {
            // Arrange
            var mockEventBus = new Mock<IEventBus>();
            var healthCheck = new EventBusHealthCheck(mockEventBus.Object);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    "test_event_bus",
                    healthCheck,
                    HealthStatus.Unhealthy,
                    new[] { "event_bus" })
            };

            // Act
            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            // Assert
            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains("operational", result.Description);
        }

        /// <summary>
        /// Tests that the EventBusHealthCheck returns Unhealthy when the event bus throws an exception.
        /// </summary>
        [Fact]
        public async Task EventBusHealthCheck_WithExceptionThrown_ShouldReturnUnhealthy()
        {
            // Arrange
            var mockEventBus = new Mock<IEventBus>();
            mockEventBus.Setup(x => x.Publish(It.IsAny<object>()))
                .Throws(new Exception("Test exception"));

            var healthCheck = new EventBusHealthCheck(mockEventBus.Object);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    "test_event_bus",
                    healthCheck,
                    HealthStatus.Unhealthy,
                    new[] { "event_bus" })
            };

            // Act
            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            // Assert
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("not operational", result.Description);
        }

        /// <summary>
        /// Tests that the EventBusHealthCheck throws an ArgumentNullException when the event bus is null.
        /// </summary>
        [Fact]
        public void EventBusHealthCheck_WithNullEventBus_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EventBusHealthCheck(null));
        }
    }
}