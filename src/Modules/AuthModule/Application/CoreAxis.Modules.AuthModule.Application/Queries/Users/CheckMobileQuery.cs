using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Queries.Users;

public record CheckMobileQuery(string MobileNumber) : IRequest<Result<CheckMobileResultDto>>;

public class CheckMobileQueryHandler : IRequestHandler<CheckMobileQuery, Result<CheckMobileResultDto>>
{
    private readonly IUserRepository _userRepository;

    public CheckMobileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<CheckMobileResultDto>> Handle(CheckMobileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByPhoneNumberAsync(request.MobileNumber, cancellationToken);
        
        var result = new CheckMobileResultDto
        {
            UserExists = user != null,
            HasPassword = user != null && !string.IsNullOrEmpty(user.PasswordHash)
        };

        return Result<CheckMobileResultDto>.Success(result);
    }
}