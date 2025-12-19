using CoreAxis.Modules.ProductBuilderModule.Application.DTOs;
using CoreAxis.Modules.ProductBuilderModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.ProductBuilderModule.Application.Queries;

public record GetProductsQuery : IRequest<Result<List<ProductDto>>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, Result<List<ProductDto>>>
{
    private readonly IProductRepository _repository;

    public GetProductsQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _repository.GetAll().ToListAsync(cancellationToken);
        
        var dtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Key = p.Key,
            Name = p.Name,
            Description = p.Description,
            IsActive = p.IsActive
        }).ToList();

        return Result<List<ProductDto>>.Success(dtos);
    }
}
