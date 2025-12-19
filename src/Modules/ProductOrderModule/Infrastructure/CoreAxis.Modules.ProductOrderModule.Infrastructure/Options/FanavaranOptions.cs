namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Options;

public class FanavaranOptions
{
    public string BaseUrl { get; set; } = "https://bime.net.iraneit.com:3023/BimeApiManager_Release/api";
    public string AppName { get; set; } = "Sales";
    public string Secret { get; set; } = "aA@12345";
    public string Username { get; set; } = "tejaratUser";
    public string Password { get; set; } = "tejaratzxc123";
    public string AuthorizationHeader { get; set; } = "Basic dGVqYXJhdFVzZXI6dGVqYXJhdHp4YzEyMw==";
    
    // Default Headers
    public string Location { get; set; } = "1035";
    public string CorpId { get; set; } = "2063";
    public string ContractId { get; set; } = "2";
}
