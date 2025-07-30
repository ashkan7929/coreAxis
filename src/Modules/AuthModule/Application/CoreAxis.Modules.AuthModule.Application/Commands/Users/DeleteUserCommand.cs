using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Users;

public record DeleteUserCommand(Guid UserId) : IRequest<Result<bool>>;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAccessLogRepository _accessLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        IAccessLogRepository accessLogRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _accessLogRepository = accessLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        
        if (user == null)
        {
            return Result<bool>.Failure("User not found");
        }

        // Check if this is the last admin user (optional business rule)
        var userRoles = await _userRepository.GetUserRolesAsync(request.UserId);
        var isAdmin = userRoles.Any(r => r.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));
        
        if (isAdmin)
        {
            var adminUsers = await _userRepository.GetUsersByRoleNameAsync("Admin");
            if (adminUsers.Count() <= 1)
            {
                return Result<bool>.Failure("Cannot delete the last admin user");
            }
        }

        // Delete access logs first
        await _accessLogRepository.DeleteByUserIdAsync(request.UserId);
        
        // Remove all user roles
        await _userRepository.RemoveAllUserRolesAsync(request.UserId);
        
        // Delete the user
        await _userRepository.DeleteAsync(request.UserId);
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}