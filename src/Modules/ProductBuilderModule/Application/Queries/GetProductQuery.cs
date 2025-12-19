using CoreAxis.Modules.ProductBuilderModule.Application.DTOs;
using CoreAxis.Modules.ProductBuilderModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.ProductBuilderModule.Application.Queries;

public record GetProductQuery(Guid Id) : IRequest<Result<ProductDto>>;

public class GetProductQueryHandler : IRequestHandler<GetProductQuery, Result<ProductDto>>
{
    private readonly IProductRepository _repository;

    public GetProductQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductDto>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id);

        if (product == null) return Result<ProductDto>.Failure("Product not found");

        return Result<ProductDto>.Success(new ProductDto
        {
            Id = product.Id,
            Key = product.Key,
            Name = product.Name,
            Description = product.Description,
            IsActive = product.IsActive
        });
    }
}
