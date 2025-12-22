using CoreAxis.EventBus;
using CoreAxis.Modules.Workflow.Api;
using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Application.EventHandlers;
using CoreAxis.Modules.Workflow.Application.Services;
using CoreAxis.Modules.MappingModule.Application.Services;
using CoreAxis.Modules.Workflow.Application.Services.StepHandlers;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using CoreAxis.SharedKernel.Context;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.Versioning;
using CoreAxis.SharedKernel.Ports;
using CoreAxis.Modules.Workflow.Application.Services.Compensation;
using CoreAxis.Modules.ApiManager.Application.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Tests.WorkflowModule;

public class FormWorkflowEndToEndTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkflowDbContext _workflowDb;
    private readonly IEventBus _eventBus;
    private readonly IWorkflowExecutor _workflowExecutor;

    public FormWorkflowEndToEndTests()
    {
        var services = new ServiceCollection();

        // 1. Add Logging
        services.AddLogging(builder => builder.AddConsole());

        // 2. Add InMemory EventBus
        services.AddSingleton<IEventBus, InMemoryEventBus>();

        // 3. Add Mocks for External Dependencies
        var dispatcherMock = new Mock<IDomainEventDispatcher>();
        services.AddSingleton(dispatcherMock.Object);

        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.TenantId).Returns("default");
        services.AddSingleton(tenantProviderMock.Object);

        var wfClientMock = new Mock<IWorkflowDefinitionClient>();
        services.AddSingleton(wfClientMock.Object);

        // Add Mock IApiProxy for ServiceTaskStepHandler
        var apiProxyMock = new Mock<IApiProxy>();
        services.AddSingleton(apiProxyMock.Object);

        // Add Mock IMappingExecutionService for ServiceTaskStepHandler
        var mappingServiceMock = new Mock<IMappingExecutionService>();
        services.AddSingleton(mappingServiceMock.Object);

        // 4. Register WorkflowModule Services
        var module = new CoreAxis.Modules.Workflow.Api.WorkflowModule();
        module.RegisterServices(services);

        // 5. Override WorkflowDbContext to use InMemory
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<WorkflowDbContext>));
        if (descriptor != null) services.Remove(descriptor);

        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<WorkflowDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        // 6. Build Provider
        _serviceProvider = services.BuildServiceProvider();

        // 7. Resolve Components
        _workflowDb = _serviceProvider.GetRequiredService<WorkflowDbContext>();
        _eventBus = _serviceProvider.GetRequiredService<IEventBus>();
        _workflowExecutor = _serviceProvider.GetRequiredService<IWorkflowExecutor>();

        // 8. Configure Application (Wiring)
        var appBuilderMock = new Mock<IApplicationBuilder>();
        appBuilderMock.Setup(x => x.ApplicationServices).Returns(_serviceProvider);
        
        module.ConfigureApplication(appBuilderMock.Object);
    }

    [Fact]
    public async Task WorkflowShouldPauseOnWaitForEvent_AndResumeOnFormSubmission_UsingWiring()
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
                LastModifiedBy = "test",
                TenantId = "default"
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

        // 3. Simulate Form Submission (via EventBus) - using WorkflowRunId explicit field
        var submissionId = Guid.NewGuid();
        
        var evt = new FormSubmitted(
            Guid.NewGuid(), // FormId
            submissionId,
            Guid.NewGuid(), // UserId
            "{}", // Data
            null, // Metadata (testing that we don't rely on it)
            Guid.NewGuid(), // CorrelationId
            workflowRunId: run.Id // Explicit WorkflowRunId
        );

        await _eventBus.PublishAsync(evt);
        
        // Clear tracker to ensure fresh data
        _workflowDb.ChangeTracker.Clear();

        // 4. Verify Workflow Resumed
        var runAfterResume = await _workflowDb.WorkflowRuns.Include(r => r.Steps).FirstAsync(r => r.Id == run.Id);
        
        // Step1 should be Completed
        var step1 = runAfterResume.Steps.First(s => s.StepId == "step1");
        Assert.Equal("Completed", step1.Status);
        
        // Workflow should be Completed
        Assert.Equal("Completed", runAfterResume.Status);
    }
}
