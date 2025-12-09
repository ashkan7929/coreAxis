using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Users;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword
) : IRequest<Result<bool>>;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        // Verify current password only if user has a password
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return Result.Failure("Current password is incorrect");
            }
        }
        // If user has no password, we skip verification (allowing them to set a password)

        // Hash new password
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        
        // Update password
        user.UpdatePassword(newPasswordHash);
        
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }
}