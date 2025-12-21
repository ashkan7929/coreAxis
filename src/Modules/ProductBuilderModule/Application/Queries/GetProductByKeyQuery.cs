using CoreAxis.Modules.ProductBuilderModule.Application.DTOs;
using CoreAxis.Modules.ProductBuilderModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.ProductBuilderModule.Application.Queries;

public record GetProductByKeyQuery(string Key) : IRequest<Result<ProductDto>>;

public class GetProductByKeyQueryHandler : IRequestHandler<GetProductByKeyQuery, Result<ProductDto>>
{
    private readonly IProductRepository _repository;

    public GetProductByKeyQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductDto>> Handle(GetProductByKeyQuery request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByKeyAsync(request.Key, cancellationToken);

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
