using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Application.Services;

public interface IShahkarService
{
    /// <summary>
    /// Verifies national code and mobile number match through Shahkar service
    /// </summary>
    /// <param name="nationalCode">National identification code</param>
    /// <param name="mobileNumber">Mobile phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating if the verification was successful</returns>
    Task<Result<bool>> VerifyNationalCodeAndMobileAsync(string nationalCode, string mobileNumber, CancellationToken cancellationToken = default);
}

public class ShahkarVerificationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TrackId { get; set; }
}