using CoreAxis.Modules.WalletModule.Application.DTOs;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
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
    /// Get all available wallet types.
    /// </summary>
    /// <remarks>
    /// Returns the list of active wallet types that can be used when creating wallets.
    ///
    /// Status codes:
    /// - 200 OK: Successful retrieval
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<WalletTypeDto>), StatusCodes.Status200OK)]
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
    /// Initialize default wallet types (for development/testing).
    /// </summary>
    /// <remarks>
    /// Populates the database with a standard set of wallet types.
    ///
    /// Status codes:
    /// - 200 OK: Initialization succeeded
    /// - 400 Bad Request: Wallet types already exist
    /// - 500 Internal Server Error: Unexpected error
    /// </remarks>
    [HttpPost("initialize")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Test endpoint to verify controller is working.
    /// </summary>
    [HttpGet("test")]
    [AllowAnonymous]
    public ActionResult Test()
    {
        return Ok(new { Message = "WalletType controller is working", Timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Create a new wallet type.
    /// </summary>
    /// <remarks>
    /// Sample request body:
    /// {
    ///   "name": "Rewards",
    ///   "description": "Wallet for reward points"
    /// }
    ///
    /// Status codes:
    /// - 201 Created: Type created
    /// - 400 Bad Request: Type with same name exists
    /// - 401 Unauthorized: Authentication required
    /// </remarks>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(WalletTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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