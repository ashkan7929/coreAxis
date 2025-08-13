using CoreAxis.Modules.WalletModule.Application.DTOs;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;

namespace CoreAxis.Modules.WalletModule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletTypeController : ControllerBase
{
    private readonly IWalletTypeRepository _walletTypeRepository;
    private readonly WalletDbContext _context;

    public WalletTypeController(IWalletTypeRepository walletTypeRepository, WalletDbContext context)
    {
        _walletTypeRepository = walletTypeRepository;
        _context = context;
    }

    /// <summary>
    /// Get all available wallet types
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<WalletTypeDto>>> GetWalletTypes()
    {
        var walletTypes = await _walletTypeRepository.GetActiveAsync();
        var result = walletTypes.Select(wt => new WalletTypeDto
        {
            Id = wt.Id,
            Name = wt.Name,
            Description = wt.Description,
            IsActive = wt.IsActive
        });
        
        return Ok(result);
    }

    /// <summary>
    /// Initialize default wallet types (for development/testing)
    /// </summary>
    [HttpPost("initialize")]
    [AllowAnonymous]
    public async Task<ActionResult> InitializeDefaultWalletTypes()
    {
        try
        {
            // First, fix any existing records with NULL audit fields using raw SQL
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE wallet.WalletTypes SET CreatedBy = 'System' WHERE CreatedBy IS NULL OR CreatedBy = ''");
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE wallet.WalletTypes SET LastModifiedBy = 'System' WHERE LastModifiedBy IS NULL OR LastModifiedBy = ''");
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE wallet.TransactionTypes SET CreatedBy = 'System' WHERE CreatedBy IS NULL OR CreatedBy = ''");
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE wallet.TransactionTypes SET LastModifiedBy = 'System' WHERE LastModifiedBy IS NULL OR LastModifiedBy = ''");
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE wallet.WalletProviders SET CreatedBy = 'System' WHERE CreatedBy IS NULL OR CreatedBy = ''");
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE wallet.WalletProviders SET LastModifiedBy = 'System' WHERE LastModifiedBy IS NULL OR LastModifiedBy = ''");
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE wallet.WalletContracts SET CreatedBy = 'System' WHERE CreatedBy IS NULL OR CreatedBy = ''");
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE wallet.WalletContracts SET LastModifiedBy = 'System' WHERE LastModifiedBy IS NULL OR LastModifiedBy = ''");

            // Update existing Wallets with NULL audit fields
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE wallet.Wallets SET CreatedBy = 'System' WHERE CreatedBy IS NULL OR CreatedBy = ''");
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE wallet.Wallets SET LastModifiedBy = 'System' WHERE LastModifiedBy IS NULL OR LastModifiedBy = ''");

            // Update existing Transactions with NULL audit fields
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE wallet.Transactions SET CreatedBy = 'System' WHERE CreatedBy IS NULL OR CreatedBy = ''");
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE wallet.Transactions SET LastModifiedBy = 'System' WHERE LastModifiedBy IS NULL OR LastModifiedBy = ''");

            // Check if wallet types already exist
            var existingTypes = await _walletTypeRepository.GetAllAsync();
            if (existingTypes.Any())
            {
                return BadRequest(new { Message = "Wallet types already exist", Count = existingTypes.Count(), ExistingTypes = existingTypes.Select(wt => new { wt.Id, wt.Name, wt.CreatedBy, wt.LastModifiedBy }) });
            }

            // Create default wallet types
            var defaultTypes = new[]
            {
                new WalletType("Main", "Primary wallet for general transactions"),
                new WalletType("Savings", "Savings wallet for long-term storage"),
                new WalletType("Commission", "Commission wallet for earned commissions"),
                new WalletType("Bonus", "Bonus wallet for promotional rewards"),
                new WalletType("Credit", "Credit wallet for credit-based transactions")
            };

            foreach (var walletType in defaultTypes)
            {
                await _walletTypeRepository.AddAsync(walletType);
            }

            await _walletTypeRepository.SaveChangesAsync();

            return Ok(new { Message = "Default wallet types created successfully", Count = defaultTypes.Length });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Test endpoint to verify controller is working
    /// </summary>
    [HttpGet("test")]
    [AllowAnonymous]
    public ActionResult Test()
    {
        return Ok(new { Message = "WalletType controller is working", Timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Create a new wallet type
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WalletTypeDto>> CreateWalletType([FromBody] CreateWalletTypeDto request)
    {
        // Check if wallet type with same name already exists
        var existingType = await _walletTypeRepository.GetByNameAsync(request.Name);
        if (existingType != null)
        {
            return BadRequest($"Wallet type with name '{request.Name}' already exists");
        }

        var walletType = new WalletType(request.Name, request.Description);
        await _walletTypeRepository.AddAsync(walletType);
        await _walletTypeRepository.SaveChangesAsync();

        var result = new WalletTypeDto
        {
            Id = walletType.Id,
            Name = walletType.Name,
            Description = walletType.Description,
            IsActive = walletType.IsActive
        };

        return CreatedAtAction(nameof(GetWalletTypes), result);
    }
}