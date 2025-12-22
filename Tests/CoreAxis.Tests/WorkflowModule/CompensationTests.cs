using CoreAxis.Modules.Workflow.Application.Services.Compensation;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using CoreAxis.SharedKernel.Context;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.Versioning;
using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.EventBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace CoreAxis.Tests.WorkflowModule;

public class CompensationTests
{
    [Fact]
    public async Task CompensateAsync_ShouldExecuteCompensationActions_WhenDefined()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var dispatcherMock = new Mock<IDomainEventDispatcher>();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.TenantId).Returns("default");
        
        var context = new WorkflowDbContext(options, dispatcherMock.Object, tenantProviderMock.Object);

        var loggerMock = new Mock<ILogger<CompensationExecutor>>();
        var apiProxyMock = new Mock<IApiProxy>();
        var eventBusMock = new Mock<IEventBus>();
        
        var executor = new CompensationExecutor(context, loggerMock.Object, apiProxyMock.Object, eventBusMock.Object);

        var runId = Guid.NewGuid();
        var defCode = "compensation-test";
        var stepId = "step1";

        // Setup Definition with Compensation
        var dsl = new
        {
            startAt = stepId,
            steps = new[]
            {
                new
                {
                    id = stepId,
                    type = "ServiceTask",
                    compensation = new[]
                    {
                        new { type = "ApiCall", config = new { url = "http://rollback" } }
                    }
                }
            }
        };

        var def = new WorkflowDefinition { Code = defCode, Name = "Test", CreatedBy = "test", LastModifiedBy = "test", TenantId = "default" };
        context.WorkflowDefinitions.Add(def);
        context.WorkflowDefinitionVersions.Add(new WorkflowDefinitionVersion
        {
            WorkflowDefinition = def,
            WorkflowDefinitionId = def.Id,
            VersionNumber = 1,
            DslJson = JsonSerializer.Serialize(dsl),
            Status = VersionStatus.Published,
            CreatedBy = "test",
            LastModifiedBy = "test"
        });

        // Setup Run with Completed Step
        var run = new WorkflowRun
        {
            Id = runId,
            WorkflowDefinitionCode = defCode,
            VersionNumber = 1,
            Status = "Failed",
            ContextJson = "{}",
            CorrelationId = "test-corr",
            CreatedBy = "test",
            LastModifiedBy = "test",
            Steps = new List<WorkflowRunStep>
            {
                new WorkflowRunStep
                {
                    StepId = stepId,
                    StepType = "ServiceTask",
                    Status = "Completed", // Completed successfully, but workflow failed later or cancelled
                    EndedAt = DateTime.UtcNow,
                    CreatedBy = "test",
                    LastModifiedBy = "test"
                }
            }
        };
        context.WorkflowRuns.Add(run);
        await context.SaveChangesAsync();

        // Act
        await executor.CompensateAsync(run, CancellationToken.None);

        // Assert
        // Verify logger was called with specific message
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Simulating API Call Compensation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
