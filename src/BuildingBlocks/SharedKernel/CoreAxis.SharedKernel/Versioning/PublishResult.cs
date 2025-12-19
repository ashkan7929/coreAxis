namespace CoreAxis.SharedKernel.Versioning;

public class PublishResult
{
    public bool Success { get; set; }
    public string? Version { get; set; }
    public string? Error { get; set; }
    public DateTime PublishedAt { get; set; }

    public static PublishResult SuccessResult(string version) => new() 
    { 
        Success = true, 
        Version = version, 
        PublishedAt = DateTime.UtcNow 
    };

    public static PublishResult Failure(string error) => new() 
    { 
        Success = false, 
        Error = error 
    };
}
