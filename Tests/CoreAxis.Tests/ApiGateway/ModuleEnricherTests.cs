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
            var mockPropertyFactory = new Mock<ILogEventPropertyFactory>();
            mockPropertyFactory.Setup(x => x.CreateProperty(It.IsAny<string>(), It.IsAny<object>(), false))
                .Returns<string, object, bool>((name, value, destructureObjects) => new LogEventProperty(name, new ScalarValue(value)));
            
            enricher.Enrich(logEvent, mockPropertyFactory.Object);

            // Assert
            // Debug: Check what properties are actually added
            var propertyKeys = string.Join(", ", logEvent.Properties.Keys);
            Console.WriteLine($"Properties added: {propertyKeys}");
            
            Assert.True(logEvent.Properties.ContainsKey("ModuleCount"), $"ModuleCount not found. Available properties: {propertyKeys}");
            Assert.True(logEvent.Properties.ContainsKey("Modules"), $"Modules not found. Available properties: {propertyKeys}");
            
            var moduleCount = (ScalarValue)logEvent.Properties["ModuleCount"];
            var moduleNames = (ScalarValue)logEvent.Properties["Modules"];
            
            Assert.Equal(3, moduleCount.Value);
            Assert.IsType<List<string>>(moduleNames.Value);
            var moduleNamesList = (List<string>)moduleNames.Value;
            Assert.Equal(3, moduleNamesList.Count);
            
            Assert.Contains("Module1", moduleNamesList);
            Assert.Contains("Module2", moduleNamesList);
            Assert.Contains("Module3", moduleNamesList);
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
            var mockPropertyFactory = new Mock<ILogEventPropertyFactory>();
            mockPropertyFactory.Setup(x => x.CreateProperty(It.IsAny<string>(), It.IsAny<object>(), false))
                .Returns<string, object, bool>((name, value, destructureObjects) => new LogEventProperty(name, new ScalarValue(value)));
            
            enricher.Enrich(logEvent, mockPropertyFactory.Object);

            // Assert
            // Debug: Check what properties are actually added
            var propertyKeys = string.Join(", ", logEvent.Properties.Keys);
            Console.WriteLine($"Properties added (empty test): {propertyKeys}");
            
            Assert.True(logEvent.Properties.ContainsKey("ModuleCount"), $"ModuleCount not found. Available properties: {propertyKeys}");
            Assert.True(logEvent.Properties.ContainsKey("Modules"), $"Modules not found. Available properties: {propertyKeys}");
            
            var moduleCount = (ScalarValue)logEvent.Properties["ModuleCount"];
            var moduleNames = (ScalarValue)logEvent.Properties["Modules"];
            
            Assert.Equal(0, moduleCount.Value);
            Assert.IsType<List<string>>(moduleNames.Value);
            Assert.Empty((List<string>)moduleNames.Value);
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
        internal class TestModule : IModule
        {
            public string Name { get; }
            public string Version => "1.0.0";

            public TestModule() : this("DefaultModule")
            {
            }

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