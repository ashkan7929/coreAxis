using CoreAxis.ApiGateway.Logging;
using CoreAxis.BuildingBlocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CoreAxis.Tests.ApiGateway
{
    /// <summary>
    /// Unit tests for the ModuleEnricher class.
    /// </summary>
    public class ModuleEnricherTests
    {
        /// <summary>
        /// Tests that the ModuleEnricher adds module count and names properties to log events.
        /// </summary>
        [Fact]
        public void Enrich_ShouldAddModuleProperties()
        {
            // Arrange
            var mockModuleRegistrar = new Mock<IModuleRegistrar>();
            var modules = new List<IModule>
            {
                new TestModule("Module1"),
                new TestModule("Module2"),
                new TestModule("Module3")
            };
            
            mockModuleRegistrar.Setup(x => x.GetRegisteredModules())
                .Returns(modules);

            var enricher = new ModuleEnricher(mockModuleRegistrar.Object);
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                MessageTemplate.Empty,
                Enumerable.Empty<LogEventProperty>());

            // Act
            enricher.Enrich(logEvent, null);

            // Assert
            Assert.True(logEvent.Properties.ContainsKey("ModuleCount"));
            Assert.True(logEvent.Properties.ContainsKey("Modules"));
            
            var moduleCount = (ScalarValue)logEvent.Properties["ModuleCount"];
            var moduleNames = (SequenceValue)logEvent.Properties["Modules"];
            
            Assert.Equal(3, moduleCount.Value);
            Assert.Equal(3, moduleNames.Elements.Count);
            
            var moduleNameValues = moduleNames.Elements
                .Select(e => ((ScalarValue)e).Value.ToString())
                .ToList();
                
            Assert.Contains("Module1", moduleNameValues);
            Assert.Contains("Module2", moduleNameValues);
            Assert.Contains("Module3", moduleNameValues);
        }

        /// <summary>
        /// Tests that the ModuleEnricher handles empty module list correctly.
        /// </summary>
        [Fact]
        public void Enrich_WithNoModules_ShouldAddEmptyProperties()
        {
            // Arrange
            var mockModuleRegistrar = new Mock<IModuleRegistrar>();
            mockModuleRegistrar.Setup(x => x.GetRegisteredModules())
                .Returns(new List<IModule>());

            var enricher = new ModuleEnricher(mockModuleRegistrar.Object);
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                MessageTemplate.Empty,
                Enumerable.Empty<LogEventProperty>());

            // Act
            enricher.Enrich(logEvent, null);

            // Assert
            Assert.True(logEvent.Properties.ContainsKey("ModuleCount"));
            Assert.True(logEvent.Properties.ContainsKey("Modules"));
            
            var moduleCount = (ScalarValue)logEvent.Properties["ModuleCount"];
            var moduleNames = (SequenceValue)logEvent.Properties["Modules"];
            
            Assert.Equal(0, moduleCount.Value);
            Assert.Empty(moduleNames.Elements);
        }

        /// <summary>
        /// Tests that the ModuleEnricher throws ArgumentNullException when moduleRegistrar is null.
        /// </summary>
        [Fact]
        public void Constructor_WithNullModuleRegistrar_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ModuleEnricher(null));
        }

        /// <summary>
        /// A test module implementation for testing purposes.
        /// </summary>
        private class TestModule : IModule
        {
            public string Name { get; }
            public string Version => "1.0.0";

            public TestModule(string name)
            {
                Name = name;
            }

            public void RegisterServices(IServiceCollection services)
            {
                // Not needed for this test
            }

            public void ConfigureApplication(IApplicationBuilder app)
            {
                // Not needed for this test
            }
        }
    }
}