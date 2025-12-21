using CoreAxis.EventBus;
using CoreAxis.Modules.TaskModule.Application.Commands;
using CoreAxis.Modules.TaskModule.Application.Handlers;
using CoreAxis.Modules.TaskModule.Infrastructure.Data;
using CoreAxis.Modules.TaskModule.Infrastructure.EventHandlers;
using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Application.EventHandlers;
using CoreAxis.Modules.Workflow.Application.Services;
using CoreAxis.Modules.Workflow.Application.Services.StepHandlers;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text.Json;
using Xunit;

using CoreAxis.Modules.Workflow.Application.Services.Compensation;

namespace CoreAxis.Tests.WorkflowModule;

public class HumanTaskEndToEndTests
{
    private readonly WorkflowDbContext _workflowDb;
    private readonly TaskDbContext _taskDb;
    private readonly InMemoryEventBus _eventBus;
    private readonly WorkflowExecutor _workflowExecutor;
    private readonly TaskCommandHandlers _taskCommandHandlers;
    private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();

    public HumanTaskEndToEndTests()
    {
        // Mock Dispatcher
        var dispatcherMock = new Mock<IDomainEventDispatcher>();

        // Setup Workflow DB
        var workflowOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _workflowDb = new WorkflowDbContext(workflowOptions, dispatcherMock.Object);

        // Setup Task DB
        var taskOptions = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _taskDb = new TaskDbContext(taskOptions, dispatcherMock.Object);

        // Setup Event Bus with Scope Factory Mock
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
            new HumanTaskStepHandler(_eventBus, new Mock<ILogger<HumanTaskStepHandler>>().Object),
            new MockStepHandler("EndStep")
        };
        _workflowExecutor = new WorkflowExecutor(_workflowDb, stepHandlers, new Mock<ICompensationExecutor>().Object, new Mock<ILogger<WorkflowExecutor>>().Object);

        // Setup Task Handlers
        _taskCommandHandlers = new TaskCommandHandlers(_taskDb, _eventBus);

        // Wiring up Event Handlers
        // 1. HumanTaskRequested -> TaskModule
        var taskRequestedHandler = new HumanTaskRequestedIntegrationEventHandler(_taskDb, new Mock<ILogger<HumanTaskRequestedIntegrationEventHandler>>().Object);
        _eventBus.Subscribe<HumanTaskRequested>(taskRequestedHandler);

        // 2. HumanTaskCompleted -> WorkflowModule
        var taskCompletedHandler = new TaskCompletedIntegrationEventHandler(_workflowExecutor, new Mock<ILogger<TaskCompletedIntegrationEventHandler>>().Object);
        _eventBus.Subscribe<HumanTaskCompleted>(taskCompletedHandler);
    }

    [Fact]
    public async Task WorkflowShouldPauseOnHumanTask_AndResumeOnApproval()
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
                    Type = "HumanTaskStep", // This triggers HumanTaskStepHandler
                    Transitions = new List<TransitionDsl> { new TransitionDsl { To = "step2" } },
                    Config = new Dictionary<string, object>
                    {
                        ["assigneeType"] = "User",
                        ["assigneeId"] = "user1"
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
                Code = "test-wf",
                Name = "Test Workflow",
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
            WorkflowDefinitionCode = "test-wf",
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

        // 2. Execute Step 1 (HumanTask)
        await _workflowExecutor.ExecuteStepAsync(run.Id, "step1");

        // Verify Workflow Paused
        var runAfterStep1 = await _workflowDb.WorkflowRuns.Include(r => r.Steps).FirstAsync(r => r.Id == run.Id);
        var step1Status = runAfterStep1.Steps.First(s => s.StepId == "step1");
        Assert.True(step1Status.Status == "Paused", $"Step failed with error: {step1Status.Error}");
        Assert.Equal("Paused", runAfterStep1.Status);

        // Verify Task Created (via EventBus -> TaskModule)
        var task = await _taskDb.TaskInstances.FirstOrDefaultAsync(t => t.WorkflowId == run.Id);
        Assert.NotNull(task);
        // Task is initially Open when assigned to User? Or Assigned?
        // TaskCommandHandlers Claim command sets it to Assigned.
        // HumanTaskRequestedIntegrationEventHandler creates it. Let's check logic there? 
        // Assuming it's created.
        
        // 3. User Approves Task
        // Command record: ApproveTaskCommand(Guid TaskId, string UserId, string? Comment, Dictionary<string, object>? Payload)
        var approveCmd = new ApproveTaskCommand(task.Id, "user1", "Looks good", new Dictionary<string, object> { ["approved"] = true });
        
        // Ensure user is assigned (if logic requires it).
        // If HumanTaskRequested set assigneeType=User, assigneeId=user1, then it should be assigned to user1.
        
        var result = await _taskCommandHandlers.Handle(approveCmd, CancellationToken.None);
        Assert.True(result.IsSuccess, $"Approve failed: {string.Join(", ", result.Errors)}");

        // Verify Task Completed
        var taskAfter = await _taskDb.TaskInstances.FindAsync(task.Id);
        Assert.Equal("Completed", taskAfter.Status);

        // 4. Verify Workflow Resumed (via EventBus -> WorkflowModule)
        // WorkflowExecutor.ResumeAsync should have been called
        
        var runAfterResume = await _workflowDb.WorkflowRuns.Include(r => r.Steps).FirstAsync(r => r.Id == run.Id);
        
        // Step1 should be Completed
        var step1 = runAfterResume.Steps.First(s => s.StepId == "step1");
        Assert.Equal("Completed", step1.Status);
        
        // Workflow should be Completed (because Step2 "EndStep" executed and finished)
        // Check if Step2 was executed
        var step2 = runAfterResume.Steps.FirstOrDefault(s => s.StepId == "step2");
        Assert.NotNull(step2);
        Assert.Equal("Completed", step2.Status);
        Assert.Equal("Completed", runAfterResume.Status);
        
        // Check context update
        Assert.Contains("approved", runAfterResume.ContextJson);
    }
}

public class MockStepHandler : IWorkflowStepHandler
{
    private readonly string _stepType;
    public MockStepHandler(string stepType) { _stepType = stepType; }
    public string StepType => _stepType;
    public Task<StepExecutionResult> ExecuteAsync(WorkflowRun run, WorkflowRunStep runStep, StepDsl step, CancellationToken cancellationToken)
    {
        return Task.FromResult(StepExecutionResult.Success());
    }
}
