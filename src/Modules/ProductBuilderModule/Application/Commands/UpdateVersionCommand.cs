using CoreAxis.Modules.ProductBuilderModule.Application.DTOs;
using CoreAxis.Modules.ProductBuilderModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Versioning;
using MediatR;

namespace CoreAxis.Modules.ProductBuilderModule.Application.Commands;

public record UpdateVersionCommand(Guid VersionId, UpdateVersionDto Dto) : IRequest<Result<bool>>;

public class UpdateVersionCommandHandler : IRequestHandler<UpdateVersionCommand, Result<bool>>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateVersionCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionAsync(request.VersionId, cancellationToken);
        if (version == null) return Result<bool>.Failure("Version not found");

        if (version.Status != VersionStatus.Draft)
            return Result<bool>.Failure("Only draft versions can be edited");

        if (request.Dto.Changelog != null)
            version.Changelog = request.Dto.Changelog;

        if (request.Dto.Binding != null)
        {
            if (version.Binding == null)
            {
                // Should have been created, but safe guard
                version.Binding = new Domain.Entities.ProductBinding { ProductVersionId = version.Id };
            }

            var b = request.Dto.Binding;
            version.Binding.WorkflowDefinitionCode = b.WorkflowDefinitionCode;
            version.Binding.WorkflowVersionNumber = b.WorkflowVersionNumber;
            version.Binding.InitialFormId = b.InitialFormId;
            version.Binding.InitialFormVersion = b.InitialFormVersion;
            version.Binding.MappingSetId = b.MappingSetId;
            version.Binding.FormulaId = b.FormulaId;
            version.Binding.FormulaVersion = b.FormulaVersion;
            version.Binding.PaymentConfigId = b.PaymentConfigId;
            version.Binding.OrderTemplateId = b.OrderTemplateId;
        }

        await _repository.UpdateVersionAsync(version, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}
