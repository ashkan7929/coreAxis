using CoreAxis.Modules.ProductBuilderModule.Application.DTOs;
using CoreAxis.Modules.ProductBuilderModule.Domain.Entities;
using CoreAxis.Modules.ProductBuilderModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.ProductBuilderModule.Application.Commands;

public record CreateProductCommand(CreateProductDto Dto) : IRequest<Result<Guid>>;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new ProductDefinition
        {
            Key = request.Dto.Key,
            Name = request.Dto.Name,
            Description = request.Dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TenantId = "default" 
        };

        await _repository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return Result<Guid>.Success(product.Id);
    }
}
