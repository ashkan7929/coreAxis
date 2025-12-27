using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Application.Services;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.Modules.Workflow.Domain.Repositories;
using CoreAxis.SharedKernel.Context;
using CoreAxis.SharedKernel.Ports;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;
using Xunit;
using CoreAxisExecutionContext = CoreAxis.SharedKernel.Context.ExecutionContext;

namespace CoreAxis.Tests.WorkflowModule;

public class WorkflowRunnerEndToEndTests
{
    private readonly Mock<IWorkflowDefinitionRepository> _repoMock;
    private readonly Mock<IApiProxy> _apiProxyMock;
    private readonly Mock<IMappingClient> _mappingMock;
    private readonly IApiManagerInvoker _invoker;
    private readonly WorkflowRunner _runner;

    public WorkflowRunnerEndToEndTests()
    {
        _repoMock = new Mock<IWorkflowDefinitionRepository>();
        _apiProxyMock = new Mock<IApiProxy>();
        _mappingMock = new Mock<IMappingClient>();
        
        _invoker = new CoreAxis.Modules.ApiManager.Application.Services.ApiManagerInvoker(
            _apiProxyMock.Object, 
            _mappingMock.Object);
        
        _runner = new WorkflowRunner(
            _repoMock.Object, 
            _invoker, 
            _mappingMock.Object);
    }

    [Fact]
    public async Task RunAsync_ExecuteAppTokenAndReturn_ShouldSucceed()
    {
        // 1. Setup DSL
        var code = "get-token-flow";
        var version = 1;
        
        var dsl = new WorkflowDsl
        {
            StartAt = "getAppToken",
            Steps = new List<StepDsl>
            {
                new StepDsl
                {
                    Id = "getAppToken",
                    Type = "apiCall",
                    Config = new Dictionary<string, object>
                    {
                        { "apiMethodRef", Guid.NewGuid().ToString() },
                        { "inputMappingSetId", Guid.NewGuid().ToString() },
                        { "outputMappingSetId", Guid.NewGuid().ToString() },
                        { "saveStepIO", true },
                        { "resultVar", "tokenResult" }
                    },
                    Transitions = new List<TransitionDsl> { new TransitionDsl { To = "returnStep" } }
                },
                new StepDsl
                {
                    Id = "returnStep",
                    Type = "return",
                    Config = new Dictionary<string, object>
                    {
                        { "outputMappingSetId", Guid.NewGuid().ToString() }
                    }
                }
            }
        };

        _repoMock.Setup(r => r.GetVersionAsync(code, version, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowDefinitionVersion
            {
                DslJson = JsonSerializer.Serialize(dsl)
            });

        // 2. Setup Mappings
        // Input Mapping for API Call
        _mappingMock.Setup(m => m.ExecuteMappingAsync(
            It.Is<Guid>(g => g.ToString() == dsl.Steps[0].Config["inputMappingSetId"].ToString()),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MappingExecutionResult(
                new Dictionary<string, string> { { "Authorization", "Basic XXX" } },
                new Dictionary<string, string>(),
                "{\"username\":\"user\"}",
                null
            ));

        // Output Mapping for API Call (extract token)
        _mappingMock.Setup(m => m.ExecuteMappingAsync(
            It.Is<Guid>(g => g.ToString() == dsl.Steps[0].Config["outputMappingSetId"].ToString()),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MappingExecutionResult(
                new(), new(), null,
                new Dictionary<string, object> { { "appToken", "12345" } } // Patch Vars
            ));
            
        // Output Mapping for Return (create final response)
        // Here we simulate that the return mapping also patches vars or maybe just verifies vars
        _mappingMock.Setup(m => m.ExecuteMappingAsync(
            It.Is<Guid>(g => g.ToString() == dsl.Steps[1].Config["outputMappingSetId"].ToString()),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MappingExecutionResult(
                new(), new(), 
                "{\"finalToken\":\"12345\"}", // Body
                new Dictionary<string, object> { { "finalOutput", "12345" } }
            ));

        // 3. Setup ApiProxy
        _apiProxyMock.Setup(p => p.InvokeWithExplicitRequestAsync(
            It.IsAny<Guid>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiProxyResult.Success(200, "{\"token\":\"12345\"}", 10));

        // 4. Run
        var context = new CoreAxisExecutionContext();
        var result = await _runner.RunAsync(code, version, context, CancellationToken.None);

        // 5. Assert
        Assert.True(result.Success);
        Assert.True(result.Context.Vars.ContainsKey("appToken"));
        Assert.Equal("12345", result.Context.Vars["appToken"].ToString());
        Assert.True(result.Context.Steps.ContainsKey("getAppToken"));
        Assert.Equal("Success", result.Context.Steps["getAppToken"].Status);
        
        // Verify 2 mappings were called for apiCall + 1 for return
        _mappingMock.Verify(m => m.ExecuteMappingAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        
        // Verify ApiProxy called with explicit parts
        _apiProxyMock.Verify(p => p.InvokeWithExplicitRequestAsync(
            It.IsAny<Guid>(),
            It.Is<Dictionary<string, string>>(h => h.ContainsKey("Authorization")),
            It.IsAny<Dictionary<string, string>>(),
            It.Is<string>(b => b.Contains("username")),
            It.IsAny<Guid?>(),
            It.Is<string>(s => s == "getAppToken"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
