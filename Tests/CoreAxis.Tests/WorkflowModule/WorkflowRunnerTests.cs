using CoreAxis.Modules.Workflow.Application.Services;
using CoreAxis.SharedKernel.Ports;
using Moq;
using Xunit;
using CoreAxisExecutionContext = CoreAxis.SharedKernel.Context.ExecutionContext;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.Modules.Workflow.Domain.Repositories;
using System.Text.Json;
using CoreAxis.Modules.Workflow.Application.DTOs.DSL;

namespace CoreAxis.Tests.WorkflowModule;

public class WorkflowRunnerTests
{
    private readonly Mock<IWorkflowDefinitionRepository> _repoMock;
    private readonly Mock<IApiManagerInvoker> _invokerMock;
    private readonly Mock<IMappingClient> _mappingMock;
    private readonly WorkflowRunner _runner;

    public WorkflowRunnerTests()
    {
        _repoMock = new Mock<IWorkflowDefinitionRepository>();
        _invokerMock = new Mock<IApiManagerInvoker>();
        _mappingMock = new Mock<IMappingClient>();
        _runner = new WorkflowRunner(_repoMock.Object, _invokerMock.Object, _mappingMock.Object);
    }

    [Fact]
    public async Task RunAsync_ShouldExecuteApiCallStep_AndReturnContext()
    {
        // Arrange
        var code = "test-flow";
        var version = 1;
        var context = new CoreAxisExecutionContext();
        
        var dsl = new WorkflowDsl
        {
            StartAt = "step1",
            Steps = new List<StepDsl>
            {
                new StepDsl
                {
                    Id = "step1",
                    Type = "apiCall",
                    Config = new Dictionary<string, object>
                    {
                        { "apiMethodRef", "method1" },
                        { "inputMappingSetId", "map1" },
                        { "outputMappingSetId", "map2" },
                        { "saveStepIO", true },
                        { "resultVar", "res" }
                    }
                }
            }
        };

        _repoMock.Setup(r => r.GetVersionAsync(code, version, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowDefinitionVersion
            {
                DslJson = JsonSerializer.Serialize(dsl)
            });

        _invokerMock.Setup(i => i.InvokeAsync(
            "method1", 
            context, 
            "map1", 
            "map2", 
            true, 
            "step1", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiInvokeResult
            {
                HttpStatusCode = 200,
                UpdatedContext = context
            });

        // Act
        var result = await _runner.RunAsync(code, version, context, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        _invokerMock.Verify(i => i.InvokeAsync("method1", context, "map1", "map2", true, "step1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
