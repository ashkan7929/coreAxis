using CoreAxis.Modules.ProductBuilderModule.Application.DTOs;
using CoreAxis.Modules.ProductBuilderModule.Domain.Entities;
using CoreAxis.Modules.ProductBuilderModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Versioning;
using MediatR;

namespace CoreAxis.Modules.ProductBuilderModule.Application.Commands;

public record CreateVersionCommand(Guid ProductId, CreateVersionDto Dto) : IRequest<Result<Guid>>;

public class CreateVersionCommandHandler : IRequestHandler<CreateVersionCommand, Result<Guid>>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateVersionCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateVersionCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.ProductId);
        if (product == null) return Result<Guid>.Failure("Product not found");

        var version = new ProductVersion
        {
            ProductId = request.ProductId,
            VersionNumber = request.Dto.VersionNumber,
            Status = VersionStatus.Draft,
            Changelog = request.Dto.Changelog,
            CreatedAt = DateTime.UtcNow,
            Binding = new ProductBinding() // Empty binding initially
        };

        await _repository.AddVersionAsync(version, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return Result<Guid>.Success(version.Id);
    }
}
