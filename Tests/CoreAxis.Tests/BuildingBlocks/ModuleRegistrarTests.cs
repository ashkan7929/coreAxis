using CoreAxis.BuildingBlocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Tests.BuildingBlocks
{
    /// <summary>
    /// Unit tests for the ModuleRegistrar.
    /// </summary>
    public class ModuleRegistrarTests
    {
        /// <summary>
        /// Tests that DiscoverModules returns all modules that implement IModule.
        /// </summary>
        [Fact]
        public void DiscoverModules_ShouldReturnAllModules()
        {
            // Arrange
            var moduleRegistrar = new ModuleRegistrar();
            var assemblies = new List<Assembly> { typeof(TestModule).Assembly };

            // Act
            var modules = moduleRegistrar.DiscoverModules(assemblies);

            // Assert
            // Should find both TestModule classes (one from ModuleRegistrarTests and one from ModuleEnricherTests)
            Assert.Equal(2, modules.Count());
            Assert.All(modules, module => Assert.IsAssignableFrom<IModule>(module));
        }

        /// <summary>
        /// Tests that RegisterModules registers all discovered modules.
        /// </summary>
        [Fact]
        public void RegisterModules_ShouldRegisterAllModules()
        {
            // Arrange
            var moduleRegistrar = new ModuleRegistrar();
            var serviceCollection = new ServiceCollection();
            var modules = new List<IModule> { new TestModule() };

            // Act
            moduleRegistrar.RegisterModules(modules, serviceCollection);

            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var registeredService = serviceProvider.GetService<ITestService>();
            Assert.NotNull(registeredService);
            Assert.IsType<TestService>(registeredService);
        }

        /// <summary>
        /// Tests that ConfigureModules configures all registered modules.
        /// </summary>
        [Fact]
        public void ConfigureModules_ShouldConfigureAllModules()
        {
            // Arrange
            var moduleRegistrar = new ModuleRegistrar();
            var modules = new List<IModule> { new TestModule() };
            var appBuilderMock = new Mock<IApplicationBuilder>();
            var configuredModules = new List<IModule>();

            appBuilderMock.Setup(x => x.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
                .Returns(appBuilderMock.Object)
                .Callback<Func<RequestDelegate, RequestDelegate>>(middleware =>
                {
                    // Simulate middleware execution
                    var nextDelegate = new RequestDelegate(context => Task.CompletedTask);
                    var currentDelegate = middleware(nextDelegate);
                });

            // Act
            moduleRegistrar.ConfigureModules(modules, appBuilderMock.Object);

            // Assert
            // Verify that Use was called at least once (for the TestModule)
            appBuilderMock.Verify(x => x.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.AtLeastOnce());
        }

        /// <summary>
        /// Tests that GetRegisteredModules returns all registered modules.
        /// </summary>
        [Fact]
        public void GetRegisteredModules_ShouldReturnAllRegisteredModules()
        {
            // Arrange
            var moduleRegistrar = new ModuleRegistrar();
            var modules = new List<IModule> { new TestModule() };
            var serviceCollection = new ServiceCollection();

            // Register modules
            moduleRegistrar.RegisterModules(modules, serviceCollection);

            // Act
            var registeredModules = moduleRegistrar.GetRegisteredModules();

            // Assert
            Assert.Single(registeredModules);
            Assert.Equal("Test Module", registeredModules.First().Name);
            Assert.Equal("1.0.0", registeredModules.First().Version);
        }
    }

    /// <summary>
    /// Test module implementation for testing purposes.
    /// </summary>
    internal class TestModule : IModule
    {
        public string Name => "Test Module";
        public string Version => "1.0.0";

        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<ITestService, TestService>();
        }

        public void ConfigureApplication(IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                // Test middleware
                await next();
            });
        }
    }

    /// <summary>
    /// Test service interface for testing purposes.
    /// </summary>
    public interface ITestService
    {
        string GetValue();
    }

    /// <summary>
    /// Test service implementation for testing purposes.
    /// </summary>
    public class TestService : ITestService
    {
        public string GetValue() => "Test Value";
    }
}