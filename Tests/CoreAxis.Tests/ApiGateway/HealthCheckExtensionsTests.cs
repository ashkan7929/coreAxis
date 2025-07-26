using CoreAxis.ApiGateway;
using CoreAxis.ApiGateway.HealthChecks;
using CoreAxis.EventBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CoreAxis.Tests.ApiGateway
{
    /// <summary>
    /// Unit tests for the HealthCheckExtensions class.
    /// </summary>
    public class HealthCheckExtensionsTests
    {
        /// <summary>
        /// Tests that AddCoreAxisHealthChecks registers the expected health checks.
        /// </summary>
        [Fact]
        public void AddCoreAxisHealthChecks_ShouldRegisterExpectedHealthChecks()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IEventBus>());

            // Act
            services.AddCoreAxisHealthChecks();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();
            
            // Get the registered health checks through reflection
            var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
            var registrations = options.Value.Registrations;
            
            // Verify self check is registered
            Assert.Contains(registrations, r => r.Name == "self");
            
            // Verify event bus check is registered
            Assert.Contains(registrations, r => r.Name == "event_bus");
            Assert.Contains(registrations, r => r.Tags.Contains("event_bus"));
        }

        /// <summary>
        /// Tests that MapCoreAxisHealthChecks maps the expected health check endpoints.
        /// </summary>
        [Fact]
        public void MapCoreAxisHealthChecks_ShouldMapExpectedEndpoints()
        {
            // Arrange
            var mockEndpointRouteBuilder = new Mock<IEndpointRouteBuilder>();
            var mockEndpointBuilder = new Mock<IEndpointConventionBuilder>();
            var endpoints = new List<string>();

            mockEndpointRouteBuilder
                .Setup(erb => erb.CreateApplicationBuilder())
                .Returns(new ApplicationBuilder(new ServiceCollection().BuildServiceProvider()));

            mockEndpointRouteBuilder
                .Setup(erb => erb.ServiceProvider)
                .Returns(new ServiceCollection().BuildServiceProvider());

            mockEndpointRouteBuilder
                .Setup(erb => erb.MapHealthChecks(It.IsAny<string>(), It.IsAny<HealthCheckOptions>()))
                .Callback<string, HealthCheckOptions>((path, _) => endpoints.Add(path))
                .Returns(mockEndpointBuilder.Object);

            // Act
            mockEndpointRouteBuilder.Object.MapCoreAxisHealthChecks();

            // Assert
            Assert.Contains("/health", endpoints);
            Assert.Contains("/health/ready", endpoints);
            Assert.Contains("/health/live", endpoints);

            mockEndpointRouteBuilder.Verify(
                erb => erb.MapHealthChecks("/health", It.IsAny<HealthCheckOptions>()),
                Times.Once);

            mockEndpointRouteBuilder.Verify(
                erb => erb.MapHealthChecks("/health/ready", It.IsAny<HealthCheckOptions>()),
                Times.Once);

            mockEndpointRouteBuilder.Verify(
                erb => erb.MapHealthChecks("/health/live", It.IsAny<HealthCheckOptions>()),
                Times.Once);
        }
    }
}