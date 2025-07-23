using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace CoreAxis.Tests.ApiGateway
{
    /// <summary>
    /// Unit tests for the health check dashboard UI.
    /// </summary>
    public class HealthDashboardTests
    {
        private const string ExpectedDashboardPath = "wwwroot/health-dashboard/index.html";

        /// <summary>
        /// Tests that the health dashboard HTML file exists and has the expected structure.
        /// </summary>
        [Fact]
        public void HealthDashboard_ShouldHaveExpectedStructure()
        {
            // Arrange
            var projectRoot = GetProjectRoot();
            var dashboardPath = Path.Combine(projectRoot, "src", "ApiGateway", "CoreAxis.ApiGateway", ExpectedDashboardPath);
            
            // Assert
            Assert.True(File.Exists(dashboardPath), $"Dashboard file not found at {dashboardPath}");
            
            // Load the HTML document
            var htmlDoc = new HtmlDocument();
            htmlDoc.Load(dashboardPath);
            
            // Check for essential elements
            Assert.NotNull(htmlDoc.DocumentNode.SelectSingleNode("//head/title"));
            Assert.NotNull(htmlDoc.DocumentNode.SelectSingleNode("//div[@id='health-status']"));
            Assert.NotNull(htmlDoc.DocumentNode.SelectSingleNode("//button[@id='refresh-btn']"));
            
            // Check for Bootstrap CSS
            var cssLinks = htmlDoc.DocumentNode.SelectNodes("//link[@rel='stylesheet']");
            Assert.NotNull(cssLinks);
            Assert.Contains(cssLinks, link => link.GetAttributeValue("href", "").Contains("bootstrap"));
            
            // Check for JavaScript
            var scripts = htmlDoc.DocumentNode.SelectNodes("//script");
            Assert.NotNull(scripts);
            
            // Check for fetch function
            var scriptContent = string.Join("\n", scripts
                .Where(s => string.IsNullOrEmpty(s.GetAttributeValue("src", "")))
                .Select(s => s.InnerText));
            
            Assert.Contains("fetch", scriptContent);
            Assert.Contains("/health", scriptContent);
        }

        /// <summary>
        /// Gets the project root directory.
        /// </summary>
        private string GetProjectRoot()
        {
            // Start from the current directory and move up until we find the solution file
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            
            return directory?.FullName ?? throw new InvalidOperationException("Could not find solution root directory");
        }
    }
}