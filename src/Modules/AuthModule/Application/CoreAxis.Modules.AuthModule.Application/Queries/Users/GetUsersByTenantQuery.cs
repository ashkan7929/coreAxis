using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Queries.Users;

public record GetUsersByTenantQuery(Guid TenantId, int PageSize = 50, int PageNumber = 1) : IRequest<Result<IEnumerable<UserDto>>>;

public class GetUsersByTenantQueryHandler : IRequestHandler<GetUsersByTenantQuery, Result<IEnumerable<UserDto>>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersByTenantQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<IEnumerable<UserDto>>> Handle(GetUsersByTenantQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetByTenantAsync(request.TenantId, cancellationToken);
        
        // Apply pagination manually since the repository method doesn't support it
        var paginatedUsers = users.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize);
        
        var userDtos = paginatedUsers.Select(user => new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsActive = user.IsActive,
            IsLocked = user.IsLocked,
            CreatedAt = user.CreatedOn,
            LastLoginAt = user.LastLoginAt,
            FailedLoginAttempts = user.FailedLoginAttempts,
            TenantId = user.TenantId ?? Guid.Empty
        }).ToList();

        return Result<IEnumerable<UserDto>>.Success(userDtos);
    }
}