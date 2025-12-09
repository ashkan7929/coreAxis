using CoreAxis.Modules.WalletModule.Application.DTOs;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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
    /// Get all wallet types (active and inactive).
    /// </summary>
    /// <remarks>
    /// Returns the complete list of wallet types available in the system.
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
        var walletTypes = await _walletTypeRepository.GetAllAsync();
        var result = walletTypes.Select(wt => new WalletTypeDto
        {
            Id = wt.Id,
            Name = wt.Name,
            Description = wt.Description,
            IsActive = wt.IsActive,
            IsDefault = wt.IsDefault
        });
        
        return Ok(result);
    }

    /// <summary>
    /// Get a wallet type by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the wallet type.</param>
    /// <returns>The wallet type details.</returns>
    /// <remarks>
    /// Returns the details of a specific wallet type.
    ///
    /// Status codes:
    /// - 200 OK: Wallet type found
    /// - 404 Not Found: Wallet type not found
    /// - 401 Unauthorized: Authentication required
    /// </remarks>
    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(WalletTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WalletTypeDto>> GetWalletTypeById([FromRoute] Guid id)
    {
        var walletType = await _walletTypeRepository.GetByIdAsync(id);
        if (walletType == null)
        {
            return NotFound("Wallet type not found");
        }

        var result = new WalletTypeDto
        {
            Id = walletType.Id,
            Name = walletType.Name,
            Description = walletType.Description,
            IsActive = walletType.IsActive,
            IsDefault = walletType.IsDefault
        };

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
                new WalletType("Main", "Primary wallet for general transactions", isDefault: true),
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

        var walletType = new WalletType(request.Name, request.Description, request.IsDefault);
        await _walletTypeRepository.AddAsync(walletType);
        await _walletTypeRepository.SaveChangesAsync();

        var result = new WalletTypeDto
        {
            Id = walletType.Id,
            Name = walletType.Name,
            Description = walletType.Description,
            IsActive = walletType.IsActive,
            IsDefault = walletType.IsDefault
        };

        return CreatedAtAction(nameof(GetWalletTypes), result);
    }

    /// <summary>
    /// Update an existing wallet type.
    /// </summary>
    /// <remarks>
    /// Updates the name/description of a wallet type and optionally toggles activation state.
    ///
    /// Sample request body:
    /// {
    ///   "name": "Savings",
    ///   "description": "Savings wallet for long-term storage",
    ///   "isActive": true
    /// }
    ///
    /// Notes:
    /// - Name must be unique (case-insensitive) and up to 100 chars.
    /// - Description up to 500 chars.
    /// - Deactivation may be restricted if wallets exist for this type.
    ///
    /// Status codes:
    /// - 200 OK: Type updated
    /// - 400 Bad Request: Validation error
    /// - 401 Unauthorized: Authentication required
    /// - 404 Not Found: Wallet type not found
    /// - 409 Conflict: Duplicate name or deactivation not allowed
    /// </remarks>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(WalletTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    // [HasPermission("WalletType", "Update")] // Uncomment when permission policy is configured
    public async Task<ActionResult<WalletTypeDto>> UpdateWalletType([FromRoute] Guid id, [FromBody] UpdateWalletTypeDto request)
    {
        // Basic input validation
        var name = (request.Name ?? string.Empty).Trim();
        var description = (request.Description ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required");
        }
        if (name.Length > 100)
        {
            return BadRequest("Name must be at most 100 characters");
        }
        if (description.Length > 500)
        {
            return BadRequest("Description must be at most 500 characters");
        }

        var walletType = await _walletTypeRepository.GetByIdAsync(id);
        if (walletType == null)
        {
            return NotFound("Wallet type not found");
        }

        // Ensure unique name (excluding current type)
        var existingWithName = await _walletTypeRepository.GetByNameAsync(name);
        if (existingWithName != null && existingWithName.Id != walletType.Id)
        {
            return Conflict($"Wallet type with name '{name}' already exists");
        }

        // Apply updates
        walletType.UpdateDetails(name, description, request.IsDefault);

        // Toggle activation if requested
        if (request.IsActive.HasValue)
        {
            if (!request.IsActive.Value)
            {
                // Prevent deactivation if any wallets exist for this type
                var hasWallets = await _context.Wallets.AnyAsync(w => w.WalletTypeId == walletType.Id);
                if (hasWallets)
                {
                    return Conflict("Cannot deactivate wallet type while wallets exist for this type");
                }
                walletType.Deactivate();
            }
            else
            {
                walletType.Activate();
            }
        }

        // Audit: set last modified by current user
        var modifiedBy = User?.Identity?.Name ?? User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
        walletType.SetLastModifiedBy(modifiedBy);

        await _walletTypeRepository.UpdateAsync(walletType);
        await _walletTypeRepository.SaveChangesAsync();

        var result = new WalletTypeDto
        {
            Id = walletType.Id,
            Name = walletType.Name,
            Description = walletType.Description,
            IsActive = walletType.IsActive,
            IsDefault = walletType.IsDefault
        };

        return Ok(result);
    }
}