using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Application.Services;

public interface ICivilRegistryService
{
    /// <summary>
    /// Gets personal information from civil registry using national code and birth date
    /// </summary>
    /// <param name="nationalCode">National identification code</param>
    /// <param name="birthDate">Birth date in YYYY-MM-DD format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Personal information from civil registry</returns>
    Task<Result<CivilRegistryPersonalInfo>> GetPersonalInfoAsync(string nationalCode, string birthDate, CancellationToken cancellationToken = default);
}

public class CivilRegistryPersonalInfo
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public int CertNumber { get; set; }
    public int Gender { get; set; } // 1 = Male, 2 = Female
    public int Aliveness { get; set; } // 1 = Alive, 0 = Deceased
    public string IdentificationSerial { get; set; } = string.Empty;
    public string IdentificationSeri { get; set; } = string.Empty;
    public string OfficeName { get; set; } = string.Empty;
    public string TrackId { get; set; } = string.Empty;
}

public class CivilRegistryResponse
{
    public CivilRegistryData Data { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
}

public class CivilRegistryData
{
    public CivilRegistryResult Result { get; set; } = new();
    public string TrackId { get; set; } = string.Empty;
}

public class CivilRegistryResult
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public int CertNumber { get; set; }
    public int Gender { get; set; }
    public int Aliveness { get; set; }
    public string IdentificationSerial { get; set; } = string.Empty;
    public string IdentificationSeri { get; set; } = string.Empty;
    public string OfficeName { get; set; } = string.Empty;
}