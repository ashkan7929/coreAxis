using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.AuthModule.Application.Queries.Users;

public record GetUsersQuery(int PageSize = 50, int PageNumber = 1) : IRequest<Result<IEnumerable<UserDto>>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<IEnumerable<UserDto>>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<IEnumerable<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAll().ToListAsync(cancellationToken);
        
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
            
        }).ToList();

        return Result<IEnumerable<UserDto>>.Success(userDtos);
    }
}