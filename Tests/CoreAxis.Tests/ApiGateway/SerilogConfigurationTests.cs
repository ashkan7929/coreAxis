using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace CoreAxis.Tests.ApiGateway
{
    /// <summary>
    /// Unit tests for the Serilog configuration.
    /// </summary>
    public class SerilogConfigurationTests
    {
        private const string SampleSerilogJson = @"{
  ""Serilog"": {
    ""MinimumLevel"": {
      ""Default"": ""Debug"",
      ""Override"": {
        ""Microsoft"": ""Information"",
        ""System"": ""Information""
      }
    },
    ""Enrich"": [ ""FromLogContext"", ""WithMachineName"", ""WithThreadId"" ],
    ""WriteTo"": [
      {
        ""Name"": ""Console"",
        ""Args"": {
          ""outputTemplate"": ""{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}{Properties:j}""
        }
      },
      {
        ""Name"": ""File"",
        ""Args"": {
          ""path"": ""logs/coreaxis-.log"",
          ""rollingInterval"": ""Day"",
          ""outputTemplate"": ""{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}{Properties:j}""
        }
      }
    ]
  }
}";

        /// <summary>
        /// Tests that the Serilog configuration can be loaded and used to create a logger.
        /// </summary>
        [Fact]
        public void SerilogConfiguration_ShouldBeValidAndLoadable()
        {
            // Arrange
            var configJson = new MemoryStream(Encoding.UTF8.GetBytes(SampleSerilogJson));
            var configuration = new ConfigurationBuilder()
                .AddJsonStream(configJson)
                .Build();

            // Act - This will throw if the configuration is invalid
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            // Assert
            Assert.NotNull(logger);
        }

        /// <summary>
        /// Tests that the Serilog configuration contains the expected minimum levels.
        /// </summary>
        [Fact]
        public void SerilogConfiguration_ShouldHaveExpectedMinimumLevels()
        {
            // Arrange
            var configJson = new MemoryStream(Encoding.UTF8.GetBytes(SampleSerilogJson));
            var configuration = new ConfigurationBuilder()
                .AddJsonStream(configJson)
                .Build();

            // Act
            var minimumLevel = configuration.GetSection("Serilog:MinimumLevel:Default").Value;
            var microsoftLevel = configuration.GetSection("Serilog:MinimumLevel:Override:Microsoft").Value;
            var systemLevel = configuration.GetSection("Serilog:MinimumLevel:Override:System").Value;

            // Assert
            Assert.Equal("Debug", minimumLevel);
            Assert.Equal("Information", microsoftLevel);
            Assert.Equal("Information", systemLevel);
        }

        /// <summary>
        /// Tests that the Serilog configuration contains the expected enrichers.
        /// </summary>
        [Fact]
        public void SerilogConfiguration_ShouldHaveExpectedEnrichers()
        {
            // Arrange
            var configJson = new MemoryStream(Encoding.UTF8.GetBytes(SampleSerilogJson));
            var configuration = new ConfigurationBuilder()
                .AddJsonStream(configJson)
                .Build();

            // Act
            var enrichers = configuration.GetSection("Serilog:Enrich").GetChildren().Select(c => c.Value).ToList();

            // Assert
            Assert.Contains("FromLogContext", enrichers);
            Assert.Contains("WithMachineName", enrichers);
            Assert.Contains("WithThreadId", enrichers);
        }

        /// <summary>
        /// Tests that the Serilog configuration contains the expected sinks.
        /// </summary>
        [Fact]
        public void SerilogConfiguration_ShouldHaveExpectedSinks()
        {
            // Arrange
            var configJson = new MemoryStream(Encoding.UTF8.GetBytes(SampleSerilogJson));
            var configuration = new ConfigurationBuilder()
                .AddJsonStream(configJson)
                .Build();

            // Act
            var sinks = configuration.GetSection("Serilog:WriteTo").GetChildren().Select(c => c.GetSection("Name").Value).ToList();

            // Assert
            Assert.Contains("Console", sinks);
            Assert.Contains("File", sinks);
        }
    }
}