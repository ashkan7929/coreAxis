namespace CoreAxis.Modules.WalletModule.Application.DTOs;

public class WalletTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateWalletTypeDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}