using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Domain.Enums;

namespace CoreAxis.Modules.AuthModule.Domain.Repositories;

public interface IOtpCodeRepository
{
    Task<OtpCode?> GetValidOtpAsync(string mobileNumber, string code, OtpPurpose purpose, CancellationToken cancellationToken = default);
    Task<OtpCode?> GetLatestOtpAsync(string mobileNumber, OtpPurpose purpose, CancellationToken cancellationToken = default);
    Task<OtpCode> AddAsync(OtpCode otpCode, CancellationToken cancellationToken = default);
    Task UpdateAsync(OtpCode otpCode, CancellationToken cancellationToken = default);
    Task<int> GetActiveOtpCountAsync(string mobileNumber, OtpPurpose purpose, TimeSpan timeWindow, CancellationToken cancellationToken = default);
    Task ExpireOldOtpsAsync(string mobileNumber, OtpPurpose purpose, CancellationToken cancellationToken = default);
    Task CleanupExpiredOtpsAsync(CancellationToken cancellationToken = default);
}