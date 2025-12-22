using CoreAxis.EventBus;
using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Application.EventHandlers;
using CoreAxis.Modules.Workflow.Application.Services;
using CoreAxis.Modules.Workflow.Application.Services.StepHandlers;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using CoreAxis.SharedKernel.Context;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

using CoreAxis.Modules.Workflow.Application.Services.Compensation;

namespace CoreAxis.Tests.WorkflowModule;

public class FormWorkflowEndToEndTests
{
    private readonly WorkflowDbContext _workflowDb;
    private readonly InMemoryEventBus _eventBus;
    private readonly WorkflowExecutor _workflowExecutor;
    private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();

    public FormWorkflowEndToEndTests()
    {
        // Mock Dispatcher
        var dispatcherMock = new Mock<IDomainEventDispatcher>();

        // Setup Workflow DB
        var workflowOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
            
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.TenantId).Returns("default");
        
        _workflowDb = new WorkflowDbContext(workflowOptions, dispatcherMock.Object, tenantProviderMock.Object);

        // Setup Event Bus
        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopeMock = new Mock<IServiceScope>();
        
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);

        _eventBus = new InMemoryEventBus(serviceProviderMock.Object, new Mock<ILogger<InMemoryEventBus>>().Object);

        // Setup Workflow Executor
        var stepHandlers = new List<IWorkflowStepHandler>
        {
            new WaitForEventStepHandler(new Mock<ILogger<WaitForEventStepHandler>>().Object),
            new MockStepHandler("EndStep")
        };
        _workflowExecutor = new WorkflowExecutor(_workflowDb, stepHandlers, new Mock<ICompensationExecutor>().Object, new Mock<ILogger<WorkflowExecutor>>().Object);

        // Wiring up Event Handlers
        var formSubmittedHandler = new FormSubmittedIntegrationEventHandler(_workflowExecutor, new Mock<ILogger<FormSubmittedIntegrationEventHandler>>().Object);
        _eventBus.Subscribe<FormSubmitted>(formSubmittedHandler);
    }

    [Fact]
    public async Task WorkflowShouldPauseOnWaitForEvent_AndResumeOnFormSubmission()
    {
        // 1. Setup Workflow Definition and Run
        var dsl = new WorkflowDsl
        {
            StartAt = "step1",
            Steps = new List<StepDsl>
            {
                new StepDsl
                {
                    Id = "step1",
                    Type = "WaitForEvent",
                    Transitions = new List<TransitionDsl> { new TransitionDsl { To = "step2" } },
                    Config = new Dictionary<string, object>
                    {
                        ["eventName"] = "FormSubmitted"
                    }
                },
                new StepDsl
                {
                    Id = "step2",
                    Type = "EndStep"
                }
            }
        };

        var defVersion = new WorkflowDefinitionVersion
        {
            Id = Guid.NewGuid(),
            WorkflowDefinition = new WorkflowDefinition 
            { 
                Code = "form-wf",
                Name = "Form Workflow",
                CreatedBy = "test",
                LastModifiedBy = "test"
            },
            VersionNumber = 1,
            DslJson = JsonSerializer.Serialize(dsl),
            Status = VersionStatus.Published,
            CreatedBy = "test",
            LastModifiedBy = "test"
        };
        _workflowDb.WorkflowDefinitionVersions.Add(defVersion);

        var run = new WorkflowRun
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionCode = "form-wf",
            VersionNumber = 1,
            ContextJson = "{}",
            Status = "Running",
            CorrelationId = Guid.NewGuid().ToString(),
            CreatedBy = "test",
            LastModifiedBy = "test"
        };
        _workflowDb.WorkflowRuns.Add(run);
        await _workflowDb.SaveChangesAsync();
        _workflowDb.ChangeTracker.Clear();

        // 2. Execute Step 1 (WaitForEvent)
        await _workflowExecutor.ExecuteStepAsync(run.Id, "step1");

        // Verify Workflow Paused
        var runAfterStep1 = await _workflowDb.WorkflowRuns.Include(r => r.Steps).FirstAsync(r => r.Id == run.Id);
        Assert.Equal("Paused", runAfterStep1.Status);
        Assert.Equal("Paused", runAfterStep1.Steps.First(s => s.StepId == "step1").Status);

        // 3. Simulate Form Submission (via EventBus)
        var submissionId = Guid.NewGuid();
        var metadata = JsonSerializer.Serialize(new { workflowRunId = run.Id });
        
        var evt = new FormSubmitted(
            Guid.NewGuid(), // FormId
            submissionId,
            Guid.NewGuid(), // UserId
            "{}", // Data
            metadata,
            Guid.NewGuid() // CorrelationId
        );

        await _eventBus.PublishAsync(evt);

        // 4. Verify Workflow Resumed
        var runAfterResume = await _workflowDb.WorkflowRuns.Include(r => r.Steps).FirstAsync(r => r.Id == run.Id);
        
        // Step1 should be Completed
        var step1 = runAfterResume.Steps.First(s => s.StepId == "step1");
        Assert.Equal("Completed", step1.Status);
        
        // Workflow should be Completed (because Step2 "EndStep" executed and finished)
        var step2 = runAfterResume.Steps.FirstOrDefault(s => s.StepId == "step2");
        Assert.NotNull(step2);
        Assert.Equal("Completed", step2.Status);
        Assert.Equal("Completed", runAfterResume.Status);
        
        // Check context update
        Assert.Contains("submissionId", runAfterResume.ContextJson);
    }
}
