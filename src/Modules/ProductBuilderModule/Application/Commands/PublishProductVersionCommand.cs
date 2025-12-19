using CoreAxis.Modules.ProductBuilderModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Versioning;
using MediatR;

namespace CoreAxis.Modules.ProductBuilderModule.Application.Commands;

public record PublishProductVersionCommand(Guid VersionId) : IRequest<Result<PublishResult>>;

public class PublishProductVersionCommandHandler : IRequestHandler<PublishProductVersionCommand, Result<PublishResult>>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PublishProductVersionCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PublishResult>> Handle(PublishProductVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionAsync(request.VersionId, cancellationToken);
        if (version == null)
            return Result<PublishResult>.Failure("Version not found");

        if (version.Status == VersionStatus.Published)
            return Result<PublishResult>.Success(PublishResult.SuccessResult(version.VersionNumber));

        version.Status = VersionStatus.Published;
        version.PublishedAt = DateTime.UtcNow;

        await _repository.UpdateVersionAsync(version, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return Result<PublishResult>.Success(PublishResult.SuccessResult(version.VersionNumber));
    }
}
