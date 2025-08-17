using CoreAxis.Modules.DynamicForm.Application.Commands.Forms;
using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Modules.DynamicForm.IntegrationTests.Controllers
{
    public class FormEventsControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly Guid _testFormId = Guid.NewGuid();

        public FormEventsControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task TriggerEvent_OnInit_ShouldReturnOk()
        {
            // Arrange
            var request = new TriggerFormEventRequest
            {
                FormId = _testFormId,
                EventType = FormEventType.OnInit,
                TenantId = "test-tenant",
                FormData = new Dictionary<string, object?>
                {
                    { "field1", "value1" },
                    { "field2", "value2" }
                },
                Metadata = new Dictionary<string, object?>
                {
                    { "source", "integration-test" }
                }
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/dynamic-forms/events/trigger", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var results = JsonConvert.DeserializeObject<List<FormEventResult>>(responseContent);
            
            Assert.NotNull(results);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task TriggerEvent_OnChange_ShouldReturnOk()
        {
            // Arrange
            var request = new TriggerFormEventRequest
            {
                FormId = _testFormId,
                EventType = FormEventType.OnChange,
                TenantId = "test-tenant",
                FormData = new Dictionary<string, object?>
                {
                    { "email", "test@example.com" },
                    { "name", "Test User" }
                },
                PreviousFormData = new Dictionary<string, object?>
                {
                    { "email", "old@example.com" },
                    { "name", "Test User" }
                },
                ChangedField = "email",
                Metadata = new Dictionary<string, object?>
                {
                    { "source", "integration-test" }
                }
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/dynamic-forms/events/trigger", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var results = JsonConvert.DeserializeObject<List<FormEventResult>>(responseContent);
            
            Assert.NotNull(results);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task TriggerEvent_BeforeSubmit_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var request = new TriggerFormEventRequest
            {
                FormId = _testFormId,
                EventType = FormEventType.BeforeSubmit,
                TenantId = "test-tenant",
                FormData = new Dictionary<string, object?>
                {
                    { "email", "test@example.com" },
                    { "name", "Test User" },
                    { "title", "Mr." }
                },
                Metadata = new Dictionary<string, object?>
                {
                    { "source", "integration-test" }
                }
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/dynamic-forms/events/trigger", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var results = JsonConvert.DeserializeObject<List<FormEventResult>>(responseContent);
            
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            Assert.All(results, r => Assert.True(r.Success));
        }

        [Fact]
        public async Task TriggerEvent_BeforeSubmit_WithInvalidData_ShouldReturnValidationErrors()
        {
            // Arrange
            var request = new TriggerFormEventRequest
            {
                FormId = _testFormId,
                EventType = FormEventType.BeforeSubmit,
                TenantId = "test-tenant",
                FormData = new Dictionary<string, object?>
                {
                    { "email", "invalid-email" }, // Invalid email
                    { "name", "" }, // Empty required field
                    { "title", "" } // Empty required field
                },
                Metadata = new Dictionary<string, object?>
                {
                    { "source", "integration-test" }
                }
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/dynamic-forms/events/trigger", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var results = JsonConvert.DeserializeObject<List<FormEventResult>>(responseContent);
            
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            
            // Should have validation errors
            var hasValidationErrors = results.Exists(r => !r.Success && r.ValidationErrors != null && r.ValidationErrors.Count > 0);
            Assert.True(hasValidationErrors);
        }

        [Fact]
        public async Task TriggerEvent_AfterSubmit_ShouldReturnOk()
        {
            // Arrange
            var request = new TriggerFormEventRequest
            {
                FormId = _testFormId,
                EventType = FormEventType.AfterSubmit,
                TenantId = "test-tenant",
                FormData = new Dictionary<string, object?>
                {
                    { "email", "test@example.com" },
                    { "name", "Test User" },
                    { "submissionId", Guid.NewGuid().ToString() }
                },
                Metadata = new Dictionary<string, object?>
                {
                    { "source", "integration-test" },
                    { "submittedAt", DateTime.UtcNow }
                }
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/dynamic-forms/events/trigger", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var results = JsonConvert.DeserializeObject<List<FormEventResult>>(responseContent);
            
            Assert.NotNull(results);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task GetHandlers_ShouldReturnHandlersInfo()
        {
            // Act
            var response = await _client.GetAsync($"/api/dynamic-forms/events/{_testFormId}/handlers");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var handlersInfo = JsonConvert.DeserializeObject<FormEventHandlersDto>(responseContent);
            
            Assert.NotNull(handlersInfo);
            Assert.Equal(_testFormId, handlersInfo.FormId);
            Assert.NotNull(handlersInfo.FormHandlers);
            Assert.NotNull(handlersInfo.GlobalHandlers);
        }

        [Fact]
        public async Task TriggerEvent_WithInvalidFormId_ShouldReturnOk()
        {
            // Arrange
            var request = new TriggerFormEventRequest
            {
                FormId = Guid.Empty, // Invalid form ID
                EventType = FormEventType.OnInit,
                TenantId = "test-tenant",
                FormData = new Dictionary<string, object?>(),
                Metadata = new Dictionary<string, object?>()
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/dynamic-forms/events/trigger", content);

            // Assert
            // Should still return OK even with no handlers registered for the form
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task TriggerEvent_WithMalformedJson_ShouldReturnBadRequest()
        {
            // Arrange
            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/dynamic-forms/events/trigger", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}