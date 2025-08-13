using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.Modules.AuthModule.Infrastructure.Data;
using CoreAxis.Modules.AuthModule.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.AuthModule.Infrastructure.Repositories;

public class OtpCodeRepository : IOtpCodeRepository
{
    private readonly AuthDbContext _context;

    public OtpCodeRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<OtpCode?> GetValidOtpAsync(string mobileNumber, string code, OtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        return await _context.OtpCodes
            .FirstOrDefaultAsync(o => 
                o.MobileNumber == mobileNumber && 
                o.Code == code && 
                o.Purpose == purpose && 
                !o.IsUsed && 
                o.ExpiresAt > DateTime.UtcNow, 
                cancellationToken);
    }

    public async Task<OtpCode?> GetLatestOtpAsync(string mobileNumber, OtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        return await _context.OtpCodes
            .Where(o => o.MobileNumber == mobileNumber && o.Purpose == purpose)
            .OrderByDescending(o => o.CreatedOn)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OtpCode> AddAsync(OtpCode otpCode, CancellationToken cancellationToken = default)
    {
        var result = await _context.OtpCodes.AddAsync(otpCode, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return result.Entity;
    }

    public async Task UpdateAsync(OtpCode otpCode, CancellationToken cancellationToken = default)
    {
        _context.OtpCodes.Update(otpCode);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetActiveOtpCountAsync(string mobileNumber, OtpPurpose purpose, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
        return await _context.OtpCodes
            .CountAsync(o => 
                o.MobileNumber == mobileNumber && 
                o.Purpose == purpose && 
                o.CreatedOn >= cutoffTime, 
                cancellationToken);
    }

    public async Task ExpireOldOtpsAsync(string mobileNumber, OtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        var oldOtps = await _context.OtpCodes
            .Where(o => 
                o.MobileNumber == mobileNumber && 
                o.Purpose == purpose && 
                !o.IsUsed && 
                o.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var otp in oldOtps)
        {
            otp.MarkAsUsed();
        }

        if (oldOtps.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CleanupExpiredOtpsAsync(CancellationToken cancellationToken = default)
    {
        var expiredOtps = await _context.OtpCodes
            .Where(o => o.ExpiresAt <= DateTime.UtcNow || o.CreatedOn <= DateTime.UtcNow.AddDays(-7))
            .ToListAsync(cancellationToken);

        if (expiredOtps.Any())
        {
            _context.OtpCodes.RemoveRange(expiredOtps);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}