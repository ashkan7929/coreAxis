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
        // Page at database level for efficiency
        var query = _userRepository.GetAll()
            .AsNoTracking()
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize);

        var userDtos = await query.Select(user => new UserDto
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
            CivilRegistryTrackId = user.CivilRegistryTrackId
        }).ToListAsync(cancellationToken);

        return Result<IEnumerable<UserDto>>.Success(userDtos);
    }
}