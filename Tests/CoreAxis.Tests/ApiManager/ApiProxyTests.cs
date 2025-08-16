using CoreAxis.Modules.ApiManager.Domain;
using Xunit;

namespace CoreAxis.Tests.ApiManager
{
    public class WebServiceTests
    {
        [Fact]
        public void WebService_Creation_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var name = "TestService";
            var baseUrl = "https://api.test.com";
            var description = "Test service description";

            // Act
            var webService = new WebService(name, baseUrl, description);

            // Assert
            Assert.Equal(name, webService.Name);
            Assert.Equal(baseUrl, webService.BaseUrl);
            Assert.Equal(description, webService.Description);
            Assert.True(webService.IsActive);
            Assert.NotEqual(Guid.Empty, webService.Id);
        }

        [Fact]
        public void WebService_Update_ShouldModifyProperties()
        {
            // Arrange
            var webService = new WebService("OldName", "https://old.com");
            var newName = "NewName";
            var newBaseUrl = "https://new.com";
            var newDescription = "New description";

            // Act
            webService.Update(newName, newBaseUrl, newDescription);

            // Assert
            Assert.Equal(newName, webService.Name);
            Assert.Equal(newBaseUrl, webService.BaseUrl);
            Assert.Equal(newDescription, webService.Description);
        }

        [Fact]
        public void WebService_Deactivate_ShouldSetIsActiveFalse()
        {
            // Arrange
            var webService = new WebService("TestService", "https://api.test.com");
            Assert.True(webService.IsActive);

            // Act
            webService.Deactivate();

            // Assert
            Assert.False(webService.IsActive);
        }

        [Fact]
        public void WebService_Activate_ShouldSetIsActiveTrue()
        {
            // Arrange
            var webService = new WebService("TestService", "https://api.test.com");
            webService.Deactivate();
            Assert.False(webService.IsActive);

            // Act
            webService.Activate();

            // Assert
            Assert.True(webService.IsActive);
        }
    }

    public class WebServiceMethodTests
    {
        [Fact]
        public void WebServiceMethod_Creation_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var webServiceId = Guid.NewGuid();
            var path = "/api/test";
            var httpMethod = "GET";
            var timeoutMs = 30000;

            // Act
            var method = new WebServiceMethod(webServiceId, path, httpMethod, timeoutMs);

            // Assert
            Assert.Equal(webServiceId, method.WebServiceId);
            Assert.Equal(path, method.Path);
            Assert.Equal(httpMethod.ToUpperInvariant(), method.HttpMethod);
            Assert.Equal(timeoutMs, method.TimeoutMs);
            Assert.True(method.IsActive);
            Assert.NotEqual(Guid.Empty, method.Id);
        }

        [Fact]
        public void WebServiceMethod_Update_ShouldModifyProperties()
        {
            // Arrange
            var webServiceId = Guid.NewGuid();
            var method = new WebServiceMethod(webServiceId, "/old", "GET", 10000);
            var newPath = "/new";
            var newHttpMethod = "POST";
            var newTimeoutMs = 20000;

            // Act
            method.Update(newPath, newHttpMethod, newTimeoutMs);

            // Assert
            Assert.Equal(newPath, method.Path);
            Assert.Equal(newHttpMethod.ToUpperInvariant(), method.HttpMethod);
            Assert.Equal(newTimeoutMs, method.TimeoutMs);
        }

        [Fact]
        public void WebServiceMethod_Deactivate_ShouldSetIsActiveFalse()
        {
            // Arrange
            var webServiceId = Guid.NewGuid();
            var method = new WebServiceMethod(webServiceId, "/test", "GET");
            Assert.True(method.IsActive);

            // Act
            method.Deactivate();

            // Assert
            Assert.False(method.IsActive);
        }
    }
}