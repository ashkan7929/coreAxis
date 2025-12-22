using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.MappingModule.Application.Services;
using CoreAxis.Modules.MappingModule.Application.DTOs;
using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Application.Services.StepHandlers;
using CoreAxis.Modules.Workflow.Application.Idempotency;
using CoreAxis.Modules.Workflow.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace CoreAxis.Tests.WorkflowModule;

public class ServiceTaskStepHandlerTests
{
    private readonly Mock<ILogger<ServiceTaskStepHandler>> _mockLogger;
    private readonly Mock<IApiProxy> _mockApiProxy;
    private readonly Mock<IMappingExecutionService> _mockMappingService;
    private readonly Mock<IIdempotencyService> _mockIdempotencyService;
    private readonly ServiceTaskStepHandler _handler;

    public ServiceTaskStepHandlerTests()
    {
        _mockLogger = new Mock<ILogger<ServiceTaskStepHandler>>();
        _mockApiProxy = new Mock<IApiProxy>();
        _mockMappingService = new Mock<IMappingExecutionService>();
        _mockIdempotencyService = new Mock<IIdempotencyService>();
        _handler = new ServiceTaskStepHandler(_mockLogger.Object, _mockApiProxy.Object, _mockMappingService.Object, _mockIdempotencyService.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepContextJsonValid_WhenExecutingServiceTask()
    {
        // Arrange
        var workflowRun = new WorkflowRun
        {
            WorkflowDefinitionCode = "test-wf",
            VersionNumber = 1,
            ContextJson = "{\"initial\": true}",
            CorrelationId = Guid.NewGuid().ToString(),
            Status = "Running"
        };
        
        var step = new StepDsl
        {
            Id = "step1",
            Type = "ServiceTaskStep",
            Config = new Dictionary<string, object>
            {
                ["serviceMethodId"] = Guid.NewGuid().ToString()
            }
        };

        var runStep = new WorkflowRunStep { StepId = step.Id };

        // Mock API success
        var apiResult = ApiProxyResult.Success(200, "{\"foo\": \"bar\"}", 100);
        _mockApiProxy.Setup(x => x.InvokeAsync(
                It.IsAny<Guid>(), 
                It.IsAny<Dictionary<string, object>>(), 
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        // Act
        var result = await _handler.ExecuteAsync(workflowRun, runStep, step, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        
        // Verify ContextJson is valid JSON and not the class name string
        // The previous bug caused it to be "CoreAxis.SharedKernel.Context.ContextDocument"
        Assert.DoesNotContain("CoreAxis.SharedKernel.Context.ContextDocument", workflowRun.ContextJson);
        
        // Should be valid JSON
        var doc = JsonDocument.Parse(workflowRun.ContextJson);
        Assert.NotNull(doc);
        
        // Verify output content (Executor would merge this)
        Assert.NotNull(result.OutputContext);
        var outputJson = JsonSerializer.Serialize(result.OutputContext);
        using var outputDoc = JsonDocument.Parse(outputJson);
        var root = outputDoc.RootElement;
        Assert.True(root.GetProperty("apis").GetProperty("step1").GetProperty("response").GetProperty("foo").GetString() == "bar");
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldKeepContextJsonValid_WhenUsingResponseMapping()
    {
        // Arrange
        var workflowRun = new WorkflowRun
        {
            WorkflowDefinitionCode = "test-wf",
            VersionNumber = 1,
            ContextJson = "{\"initial\": true}",
            CorrelationId = Guid.NewGuid().ToString(),
            Status = "Running"
        };

        var methodId = Guid.NewGuid();
        var mappingId = Guid.NewGuid();
        
        var step = new StepDsl
        {
            Id = "step1",
            Type = "ServiceTaskStep",
            Config = new Dictionary<string, object>
            {
                ["serviceMethodId"] = methodId.ToString(),
                ["responseMappingId"] = mappingId.ToString()
            }
        };

        var runStep = new WorkflowRunStep { StepId = step.Id };

        // Mock API success
        var apiResult = ApiProxyResult.Success(200, "{\"foo\": \"bar\"}", 100);
        _mockApiProxy.Setup(x => x.InvokeAsync(
                methodId, 
                It.IsAny<Dictionary<string, object>>(), 
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);
            
        // Mock Mapping
        var mappedOutput = "{\"mapped\": \"value\"}";
        _mockMappingService.Setup(x => x.ExecuteMappingAsync(mappingId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestMappingResponseDto { Success = true, OutputJson = mappedOutput });

        // Act
        var result = await _handler.ExecuteAsync(workflowRun, runStep, step, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        
        // Verify ContextJson is valid JSON
        Assert.DoesNotContain("CoreAxis.SharedKernel.Context.ContextDocument", workflowRun.ContextJson);
        
        // Should contain mapped result in OutputContext
        Assert.NotNull(result.OutputContext);
        using var outDoc = JsonDocument.Parse(JsonSerializer.Serialize(result.OutputContext));
        var outRoot = outDoc.RootElement;
        Assert.True(outRoot.GetProperty("mapped").GetString() == "value");
    }
}
