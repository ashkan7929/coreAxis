using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.ProductBuilderModule.Application.Queries;

public record GetProductDependenciesQuery(Guid VersionId) : IRequest<Result<object>>; // Returning object/json for graph

public class GetProductDependenciesQueryHandler : IRequestHandler<GetProductDependenciesQuery, Result<object>>
{
    public Task<Result<object>> Handle(GetProductDependenciesQuery request, CancellationToken cancellationToken)
    {
        // Mock response for dependency graph
        var graph = new
        {
            VersionId = request.VersionId,
            Nodes = new[]
            {
                new { Id = "Workflow", Type = "Workflow", Status = "Resolved" },
                new { Id = "Form", Type = "Form", Status = "Resolved" }
            },
            Edges = new[]
            {
                new { From = "Product", To = "Workflow" },
                new { From = "Product", To = "Form" }
            }
        };
        
        return Task.FromResult(Result<object>.Success(graph));
    }
}
