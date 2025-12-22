namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Options;

public class FanavaranOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AuthorizationHeader { get; set; } = string.Empty;
    
    // Default Headers
    public string Location { get; set; } = string.Empty;
    public string CorpId { get; set; } = string.Empty;
    public string ContractId { get; set; } = string.Empty;
}
