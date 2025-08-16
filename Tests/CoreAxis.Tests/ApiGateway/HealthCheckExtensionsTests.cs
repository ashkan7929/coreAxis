using CoreAxis.ApiGateway;
using CoreAxis.ApiGateway.HealthChecks;
using HealthChecksExtensions = CoreAxis.ApiGateway.HealthChecks.HealthChecksExtensions;
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
            
            // Verify api gateway health check is registered
            Assert.Contains(registrations, r => r.Name == "api_gateway_health_check");
        }

        /// <summary>
        /// Tests that MapCoreAxisHealthChecks extension method exists and can be called.
        /// Note: This is a basic smoke test since extension methods cannot be easily mocked.
        /// </summary>
        [Fact]
        public void MapCoreAxisHealthChecks_ShouldExistAsExtensionMethod()
        {
            // Arrange & Act & Assert - This should not throw an exception
            // The method should be available as an extension method
            var methodExists = typeof(HealthChecksExtensions)
                .GetMethods()
                .Any(m => m.Name == "MapCoreAxisHealthChecks" && m.IsStatic);
            
            Assert.True(methodExists, "MapCoreAxisHealthChecks extension method should exist");
        }
    }
}