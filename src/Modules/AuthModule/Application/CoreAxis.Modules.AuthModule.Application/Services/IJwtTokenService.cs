using CoreAxis.Modules.AuthModule.Domain.Entities;

namespace CoreAxis.Modules.AuthModule.Application.Services;

public interface IJwtTokenService
{
    Task<TokenResult> GenerateTokenAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Guid?> GetUserIdFromTokenAsync(string token, CancellationToken cancellationToken = default);
}

public class TokenResult
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}