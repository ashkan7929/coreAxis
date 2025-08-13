using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Queries.Users;

public record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserDto>>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        
        if (user == null)
        {
            return Result<UserDto>.Failure("User not found");
        }

        // Get user with roles and permissions
        var userWithPermissions = await _userRepository.GetWithPermissionsAsync(user.Id, cancellationToken);
        if (userWithPermissions == null)
        {
            return Result<UserDto>.Failure("Failed to load user permissions");
        }

        // Map roles with permissions
        var roleDtos = userWithPermissions.UserRoles.Select(ur => new RoleDto
        {
            Id = ur.Role.Id,
            Name = ur.Role.Name,
            Description = ur.Role.Description,
            IsActive = ur.Role.IsActive,
            IsSystemRole = ur.Role.IsSystemRole,
            CreatedAt = ur.Role.CreatedOn,
            Permissions = ur.Role.RolePermissions.Select(rp => new PermissionDto
            {
                Id = rp.Permission.Id,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
                IsActive = rp.Permission.IsActive,
                CreatedAt = rp.Permission.CreatedOn,
                Page = rp.Permission.Page != null ? new PageDto
                {
                    Id = rp.Permission.Page.Id,
                    Code = rp.Permission.Page.Code,
                    Name = rp.Permission.Page.Name,
                    Description = rp.Permission.Page.Description,
                    Path = rp.Permission.Page.Path,
                    ModuleName = rp.Permission.Page.ModuleName,
                    IsActive = rp.Permission.Page.IsActive,
                    SortOrder = rp.Permission.Page.SortOrder,
                    CreatedAt = rp.Permission.Page.CreatedOn
                } : null,
                Action = rp.Permission.Action != null ? new ActionDto
                {
                    Id = rp.Permission.Action.Id,
                    Code = rp.Permission.Action.Code,
                    Name = rp.Permission.Action.Name,
                    Description = rp.Permission.Action.Description,
                    IsActive = rp.Permission.Action.IsActive,
                    SortOrder = rp.Permission.Action.SortOrder,
                    CreatedAt = rp.Permission.Action.CreatedOn
                } : null
            }).ToList()
        }).ToList();

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsActive = user.IsActive,
            IsLocked = user.IsLocked,
            CreatedAt = user.CreatedOn,
            LastLoginAt = user.LastLoginAt,
            FailedLoginAttempts = user.FailedLoginAttempts,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FatherName = user.FatherName,
            BirthDate = user.BirthDate,
            Gender = user.Gender,
            CertNumber = user.CertNumber,
            IdentificationSerial = user.IdentificationSerial,
            IdentificationSeri = user.IdentificationSeri,
            OfficeName = user.OfficeName,
            ReferralCode = user.ReferralCode,
            PhoneNumber = user.PhoneNumber,
            NationalCode = user.NationalCode,
            IsMobileVerified = user.IsMobileVerified,
            IsNationalCodeVerified = user.IsNationalCodeVerified,
            IsPersonalInfoVerified = user.IsPersonalInfoVerified,
            CivilRegistryTrackId = user.CivilRegistryTrackId,
            Roles = roleDtos
        };

        return Result<UserDto>.Success(userDto);
    }
}