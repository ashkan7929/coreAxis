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

    public CreateVersionCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateVersionCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.ProductId);
        if (product == null) return Result<Guid>.Failure("Product not found");

        var existingVersion = await _repository.GetVersionByNumberAsync(request.ProductId, request.Dto.VersionNumber, cancellationToken);
        if (existingVersion != null)
        {
            // Idempotency: Update changelog if provided and different
            if (!string.IsNullOrEmpty(request.Dto.Changelog) && existingVersion.Changelog != request.Dto.Changelog)
            {
                existingVersion.Changelog = request.Dto.Changelog;
                await _repository.UpdateVersionAsync(existingVersion, cancellationToken);
                await _repository.UnitOfWork.SaveChangesAsync();
            }
            return Result<Guid>.Success(existingVersion.Id);
        }

        var version = new ProductVersion
        {
            ProductId = request.ProductId,
            VersionNumber = request.Dto.VersionNumber,
            Status = VersionStatus.Draft,
            Changelog = request.Dto.Changelog,
            CreatedAt = DateTime.UtcNow,
            Binding = new ProductBinding 
            { 
                CreatedBy = "system",
                LastModifiedBy = "system"
            },
            CreatedBy = "system",
            LastModifiedBy = "system"
        };

        await _repository.AddVersionAsync(version, cancellationToken);
        await _repository.UnitOfWork.SaveChangesAsync();

        return Result<Guid>.Success(version.Id);
    }
}
