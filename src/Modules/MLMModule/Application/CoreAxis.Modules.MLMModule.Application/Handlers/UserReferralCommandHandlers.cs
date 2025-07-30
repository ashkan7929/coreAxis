using CoreAxis.Modules.MLMModule.Application.Commands;
using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.MLMModule.Application.Handlers;

public class CreateUserReferralCommandHandler : IRequestHandler<CreateUserReferralCommand, UserReferralDto>
{
    private readonly IUserReferralRepository _userReferralRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserReferralCommandHandler(
        IUserReferralRepository userReferralRepository,
        IUnitOfWork unitOfWork)
    {
        _userReferralRepository = userReferralRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserReferralDto> Handle(CreateUserReferralCommand request, CancellationToken cancellationToken)
    {
        // Check if user already has a referral record
        var existingReferral = await _userReferralRepository.GetByUserIdAsync(request.UserId);
        if (existingReferral != null)
        {
            throw new InvalidOperationException("User already has a referral record");
        }

        // Get parent referral if specified
        UserReferral? parentReferral = null;
        if (request.ParentUserId.HasValue)
        {
            parentReferral = await _userReferralRepository.GetByUserIdAsync(request.ParentUserId.Value);
            if (parentReferral == null)
            {
                throw new InvalidOperationException("Parent user referral not found");
            }
        }

        // Calculate level and path
        var level = parentReferral?.Level + 1 ?? 1;
        var path = parentReferral != null ? $"{parentReferral.Path}/{request.UserId}" : $"/{request.UserId}";

        var userReferral = new UserReferral(
            request.UserId,
            request.ParentUserId);
        
        userReferral.SetPath(path, level);

        await _userReferralRepository.AddAsync(userReferral);
        await _unitOfWork.SaveChangesAsync();

        return new UserReferralDto
        {
            Id = userReferral.Id,
            UserId = userReferral.UserId,
            ParentUserId = userReferral.ParentUserId,
            Path = userReferral.Path,
            Level = userReferral.Level,
            IsActive = userReferral.IsActive,
            JoinedAt = userReferral.JoinedAt
        };
    }
}

public class UpdateUserReferralCommandHandler : IRequestHandler<UpdateUserReferralCommand, UserReferralDto>
{
    private readonly IUserReferralRepository _userReferralRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserReferralCommandHandler(
        IUserReferralRepository userReferralRepository,
        IUnitOfWork unitOfWork)
    {
        _userReferralRepository = userReferralRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserReferralDto> Handle(UpdateUserReferralCommand request, CancellationToken cancellationToken)
    {
        var userReferral = await _userReferralRepository.GetByIdAsync(request.Id);
        if (userReferral == null)
        {
            throw new InvalidOperationException("User referral not found");
        }

        // Update parent if specified and different
        if (request.ParentUserId != userReferral.ParentUserId)
        {
            UserReferral? newParent = null;
            if (request.ParentUserId.HasValue)
            {
                newParent = await _userReferralRepository.GetByUserIdAsync(request.ParentUserId.Value);
                if (newParent == null)
                {
                    throw new InvalidOperationException("New parent user referral not found");
                }
            }

            // Calculate new level and path
            var newLevel = newParent?.Level + 1 ?? 1;
            var newPath = newParent != null ? $"{newParent.Path}/{userReferral.UserId}" : $"/{userReferral.UserId}";

            userReferral.SetPath(newPath, newLevel);
        }

        await _userReferralRepository.UpdateAsync(userReferral);
        await _unitOfWork.SaveChangesAsync();

        return new UserReferralDto
        {
            Id = userReferral.Id,
            UserId = userReferral.UserId,
            ParentUserId = userReferral.ParentUserId,
            Path = userReferral.Path,
            Level = userReferral.Level,
            IsActive = userReferral.IsActive,
            JoinedAt = userReferral.JoinedAt
        };
    }
}

public class ActivateUserReferralCommandHandler : IRequestHandler<ActivateUserReferralCommand, bool>
{
    private readonly IUserReferralRepository _userReferralRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateUserReferralCommandHandler(
        IUserReferralRepository userReferralRepository,
        IUnitOfWork unitOfWork)
    {
        _userReferralRepository = userReferralRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ActivateUserReferralCommand request, CancellationToken cancellationToken)
    {
        var userReferral = await _userReferralRepository.GetByUserIdAsync(request.UserId);
        if (userReferral == null)
        {
            return false;
        }

        userReferral.Activate();
        await _userReferralRepository.UpdateAsync(userReferral);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}

public class DeactivateUserReferralCommandHandler : IRequestHandler<DeactivateUserReferralCommand, bool>
{
    private readonly IUserReferralRepository _userReferralRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateUserReferralCommandHandler(
        IUserReferralRepository userReferralRepository,
        IUnitOfWork unitOfWork)
    {
        _userReferralRepository = userReferralRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeactivateUserReferralCommand request, CancellationToken cancellationToken)
    {
        var userReferral = await _userReferralRepository.GetByUserIdAsync(request.UserId);
        if (userReferral == null)
        {
            return false;
        }

        userReferral.Deactivate();
        await _userReferralRepository.UpdateAsync(userReferral);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}

public class DeleteUserReferralCommandHandler : IRequestHandler<DeleteUserReferralCommand, bool>
{
    private readonly IUserReferralRepository _userReferralRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserReferralCommandHandler(
        IUserReferralRepository userReferralRepository,
        IUnitOfWork unitOfWork)
    {
        _userReferralRepository = userReferralRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteUserReferralCommand request, CancellationToken cancellationToken)
    {
        var userReferral = await _userReferralRepository.GetByIdAsync(request.Id);
        if (userReferral == null)
        {
            return false;
        }

        // Check if user has children - prevent deletion if they do
        var children = await _userReferralRepository.GetChildrenAsync(userReferral.UserId);
        if (children.Any())
        {
            throw new InvalidOperationException("Cannot delete user referral with existing children");
        }

        await _userReferralRepository.DeleteAsync(userReferral);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}