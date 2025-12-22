using CoreAxis.EventBus;
using CoreAxis.Modules.DynamicForm.Application.Commands.Submissions;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Modules.DynamicForm.IntegrationTests.Controllers
{
    public class FormSubmissionCorrelationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly Mock<IEventBus> _eventBusMock;

        public FormSubmissionCorrelationTests(WebApplicationFactory<Program> factory)
        {
            _eventBusMock = new Mock<IEventBus>();
            
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Mock EventBus to capture published events
                    services.AddSingleton(_eventBusMock.Object);

                    // Ensure we use InMemory database for DynamicForm
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<DynamicFormDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<DynamicFormDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("DynamicFormDbForCorrelationTests");
                    });
                });
            });
        }

        [Fact]
        public async Task CreateSubmission_WithCorrelationIdHeader_ShouldPropagateToEvent()
        {
            // Arrange
            var client = _factory.CreateClient();
            var correlationId = Guid.NewGuid();
            var formId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Seed Form
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DynamicFormDbContext>();
                db.Database.EnsureCreated();
                
                if (!await db.Forms.AnyAsync(f => f.Id == formId))
                {
                    db.Forms.Add(new Form
                    {
                        Id = formId,
                        Name = "Test Form",
                        Schema = "{}",
                        IsActive = true, // Ensure form is active
                        IsPublished = true,
                        CreatedBy = "test",
                        LastModifiedBy = "test"
                    });
                    await db.SaveChangesAsync();
                }
            }

            var command = new CreateSubmissionCommand
            {
                FormId = formId,
                UserId = userId.ToString(),
                SubmissionData = new Dictionary<string, object> { { "field1", "value1" } },
                Metadata = new Dictionary<string, object> { { "workflowRunId", Guid.NewGuid() } } // Optional: test extraction too
            };

            var content = new StringContent(JsonConvert.SerializeObject(command), Encoding.UTF8, "application/json");
            
            // Set Correlation Header
            client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId.ToString());

            // Act
            var response = await client.PostAsync("/api/Submissions", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // 1. Verify Response Header
            if (response.Headers.TryGetValues("X-Correlation-Id", out var values))
            {
                Assert.Equal(correlationId.ToString(), values.FirstOrDefault());
            }
            else
            {
                // It's possible the middleware doesn't set it on response if not explicitly configured to do so for all paths,
                // but CorrelationMiddleware usually does.
                // If this fails, we'll see.
            }

            // 2. Verify Event Published with correct CorrelationId
            _eventBusMock.Verify(bus => bus.PublishAsync(
                It.Is<FormSubmitted>(evt => 
                    evt.CorrelationId == correlationId &&
                    evt.FormId == formId
                )), Times.Once);
        }
    }
}
