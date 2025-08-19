using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.MLMModule.Application.Services;

public class CommissionRuleSetService : ICommissionRuleSetService
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;
    private readonly ILogger<CommissionRuleSetService> _logger;

    public CommissionRuleSetService(
        ICommissionRuleSetRepository ruleSetRepository,
        ILogger<CommissionRuleSetService> logger)
    {
        _ruleSetRepository = ruleSetRepository;
        _logger = logger;
    }

    public async Task<Result<CommissionRuleDto>> CreateRuleSetAsync(
        CreateCommissionRuleSetDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate unique code
            var code = GenerateRuleSetCode(dto.Name);
            
            var ruleSet = new CommissionRuleSet(
                code,
                dto.Name,
                dto.Description,
                dto.MaxLevels);

            ruleSet.UpdateMinimumPurchaseAmount(dto.MinimumPurchaseAmount);
            ruleSet.UpdateRequireActiveUpline(dto.RequireActiveUpline);

            // Add commission levels
            foreach (var levelDto in dto.CommissionLevels)
            {
                ruleSet.AddCommissionLevel(
                    levelDto.Level,
                    levelDto.Percentage,
                    levelDto.FixedAmount,
                    levelDto.MaxAmount,
                    levelDto.MinAmount);
            }

            await _ruleSetRepository.AddAsync(ruleSet);
            
            _logger.LogInformation("Created commission rule set {RuleSetId} with code {Code}", 
                ruleSet.Id, ruleSet.Code);

            return Result<CommissionRuleDto>.Success(MapToDto(ruleSet));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating commission rule set");
            return Result<CommissionRuleDto>.Failure("Failed to create commission rule set");
        }
    }

    public async Task<Result<CommissionRuleDto>> UpdateRuleSetAsync(
        Guid id,
        UpdateCommissionRuleSetDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ruleSet = await _ruleSetRepository.GetByIdAsync(id, cancellationToken);
            if (ruleSet == null)
            {
                return Result<CommissionRuleDto>.Failure("Commission rule set not found");
            }

            ruleSet.UpdateName(dto.Name);
            ruleSet.UpdateDescription(dto.Description);
            ruleSet.UpdateMaxLevels(dto.MaxLevels);
            ruleSet.UpdateMinimumPurchaseAmount(dto.MinimumPurchaseAmount);
            ruleSet.UpdateRequireActiveUpline(dto.RequireActiveUpline);

            if (dto.IsActive)
                ruleSet.Activate();
            else
                ruleSet.Deactivate();

            // Clear existing levels and add new ones
            ruleSet.ClearCommissionLevels();
            foreach (var levelDto in dto.CommissionLevels)
            {
                ruleSet.AddCommissionLevel(
                    levelDto.Level,
                    levelDto.Percentage,
                    levelDto.FixedAmount,
                    levelDto.MaxAmount,
                    levelDto.MinAmount);
            }

            await _ruleSetRepository.UpdateAsync(ruleSet);
            
            _logger.LogInformation("Updated commission rule set {RuleSetId}", ruleSet.Id);

            return Result<CommissionRuleDto>.Success(MapToDto(ruleSet));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating commission rule set {RuleSetId}", id);
            return Result<CommissionRuleDto>.Failure("Failed to update commission rule set");
        }
    }

    public async Task<Result<CommissionRuleDto>> GetRuleSetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ruleSet = await _ruleSetRepository.GetByIdAsync(id, cancellationToken);
            if (ruleSet == null)
            {
                return Result<CommissionRuleDto>.Failure("Commission rule set not found");
            }

            return Result<CommissionRuleDto>.Success(MapToDto(ruleSet));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting commission rule set {RuleSetId}", id);
            return Result<CommissionRuleDto>.Failure("Failed to get commission rule set");
        }
    }

    public async Task<Result<IEnumerable<CommissionRuleDto>>> GetAllRuleSetsAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IEnumerable<CommissionRuleSet> ruleSets;
            
            if (isActive.HasValue)
            {
                ruleSets = await _ruleSetRepository.GetActiveAsync(cancellationToken);
            }
            else
            {
                ruleSets = await _ruleSetRepository.GetAllAsync(0, 1000, cancellationToken);
            }

            var dtos = ruleSets.Select(MapToDto);
            return Result<IEnumerable<CommissionRuleDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting commission rule sets");
            return Result<IEnumerable<CommissionRuleDto>>.Failure("Failed to get commission rule sets");
        }
    }

    public async Task<Result<CommissionRuleDto>> GetDefaultRuleSetAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ruleSet = await _ruleSetRepository.GetDefaultAsync(cancellationToken);
            if (ruleSet == null)
            {
                return Result<CommissionRuleDto>.Failure("No default commission rule set found");
            }

            return Result<CommissionRuleDto>.Success(MapToDto(ruleSet));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default commission rule set");
            return Result<CommissionRuleDto>.Failure("Failed to get default commission rule set");
        }
    }

    public async Task<Result<CommissionRuleDto>> GetRuleSetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ruleSet = await _ruleSetRepository.GetByProductIdAsync(productId, cancellationToken);
            if (ruleSet == null)
            {
                // Fallback to default rule set
                return await GetDefaultRuleSetAsync(cancellationToken);
            }

            return Result<CommissionRuleDto>.Success(MapToDto(ruleSet));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting commission rule set for product {ProductId}", productId);
            return Result<CommissionRuleDto>.Failure("Failed to get commission rule set for product");
        }
    }

    public async Task<Result<bool>> ActivateRuleSetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ruleSet = await _ruleSetRepository.GetByIdAsync(id, cancellationToken);
            if (ruleSet == null)
            {
                return Result.Failure("Commission rule set not found");
            }

            ruleSet.Activate();
            await _ruleSetRepository.UpdateAsync(ruleSet);
            
            _logger.LogInformation("Activated commission rule set {RuleSetId}", ruleSet.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating commission rule set {RuleSetId}", id);
            return Result.Failure("Failed to activate commission rule set");
        }
    }

    public async Task<Result<bool>> DeactivateRuleSetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ruleSet = await _ruleSetRepository.GetByIdAsync(id, cancellationToken);
            if (ruleSet == null)
            {
                return Result.Failure("Commission rule set not found");
            }

            ruleSet.Deactivate();
            await _ruleSetRepository.UpdateAsync(ruleSet);
            
            _logger.LogInformation("Deactivated commission rule set {RuleSetId}", ruleSet.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating commission rule set {RuleSetId}", id);
            return Result.Failure("Failed to deactivate commission rule set");
        }
    }

    public async Task<Result<bool>> SetAsDefaultAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ruleSet = await _ruleSetRepository.GetByIdAsync(id, cancellationToken);
            if (ruleSet == null)
            {
                return Result.Failure("Commission rule set not found");
            }

            // First, unset any existing default
            var currentDefault = await _ruleSetRepository.GetDefaultAsync(cancellationToken);
            if (currentDefault != null && currentDefault.Id != id)
            {
                currentDefault.RemoveAsDefault();
                await _ruleSetRepository.UpdateAsync(currentDefault);
            }

            // Set new default
            ruleSet.SetAsDefault();
            await _ruleSetRepository.UpdateAsync(ruleSet);
            
            _logger.LogInformation("Set commission rule set {RuleSetId} as default", ruleSet.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting commission rule set {RuleSetId} as default", id);
            return Result.Failure("Failed to set commission rule set as default");
        }
    }

    public async Task<Result<bool>> DeleteRuleSetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ruleSet = await _ruleSetRepository.GetByIdAsync(id, cancellationToken);
            if (ruleSet == null)
            {
                return Result.Failure("Commission rule set not found");
            }

            if (ruleSet.IsDefault)
            {
                return Result.Failure("Cannot delete default commission rule set");
            }

            await _ruleSetRepository.DeleteAsync(ruleSet);
            
            _logger.LogInformation("Deleted commission rule set {RuleSetId}", ruleSet.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting commission rule set {RuleSetId}", id);
            return Result.Failure("Failed to delete commission rule set");
        }
    }

    public async Task<Result<CommissionRuleVersionDto>> PublishVersionAsync(
        Guid ruleSetId,
        string schemaJson,
        string publishedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ruleSet = await _ruleSetRepository.GetByIdAsync(ruleSetId, cancellationToken);
            if (ruleSet == null)
            {
                return Result<CommissionRuleVersionDto>.Failure("Commission rule set not found");
            }

            // Validate schema JSON
            var validationResult = await ValidateSchemaAsync(schemaJson, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return Result<CommissionRuleVersionDto>.Failure("Invalid schema JSON");
            }

            var version = ruleSet.PublishVersion(schemaJson, publishedBy);
            await _ruleSetRepository.UpdateAsync(ruleSet);
            
            _logger.LogInformation("Published version {Version} for commission rule set {RuleSetId}", 
                version.Version, ruleSet.Id);

            return Result<CommissionRuleVersionDto>.Success(MapVersionToDto(version));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing version for commission rule set {RuleSetId}", ruleSetId);
            return Result<CommissionRuleVersionDto>.Failure("Failed to publish version");
        }
    }

    public async Task<Result<IEnumerable<CommissionRuleVersionDto>>> GetVersionsAsync(
        Guid ruleSetId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ruleSet = await _ruleSetRepository.GetByIdAsync(ruleSetId, cancellationToken);
            if (ruleSet == null)
            {
                return Result<IEnumerable<CommissionRuleVersionDto>>.Failure("Commission rule set not found");
            }

            var versions = ruleSet.Versions.Select(MapVersionToDto);
            return Result<IEnumerable<CommissionRuleVersionDto>>.Success(versions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting versions for commission rule set {RuleSetId}", ruleSetId);
            return Result<IEnumerable<CommissionRuleVersionDto>>.Failure("Failed to get versions");
        }
    }

    public async Task<Result<bool>> ValidateSchemaAsync(
        string schemaJson,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic JSON validation
            if (string.IsNullOrWhiteSpace(schemaJson))
            {
                return Result<bool>.Failure("Schema JSON cannot be empty");
            }

            // Try to parse as JSON
            try
            {
                JsonDocument.Parse(schemaJson);
            }
            catch (JsonException)
            {
                return Result<bool>.Failure("Invalid JSON format");
            }

            // Additional schema validation logic can be added here
            // For example, validate against a JSON schema

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating schema JSON");
            return Result<bool>.Failure("Failed to validate schema");
        }
    }

    public async Task<Result<ProductRuleBindingDto>> AddProductBindingAsync(
        Guid ruleSetId,
        CreateProductRuleBindingDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ruleSet = await _ruleSetRepository.GetByIdAsync(ruleSetId, cancellationToken);
            if (ruleSet == null)
            {
                return Result<ProductRuleBindingDto>.Failure("Commission rule set not found");
            }

            var binding = new ProductRuleBinding(ruleSetId, dto.ProductId);
            
            if (dto.ValidFrom.HasValue || dto.ValidTo.HasValue)
            {
                binding.SetValidityPeriod(dto.ValidFrom, dto.ValidTo);
            }
            
            ruleSet.AddProductBinding(binding);
            await _ruleSetRepository.UpdateAsync(ruleSet);
            
            _logger.LogInformation("Added product binding for product {ProductId} to rule set {RuleSetId}", 
                dto.ProductId, ruleSetId);

            return Result<ProductRuleBindingDto>.Success(MapBindingToDto(binding));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product binding to rule set {RuleSetId}", ruleSetId);
            return Result<ProductRuleBindingDto>.Failure("Failed to add product binding");
        }
    }

    public async Task<Result<bool>> RemoveProductBindingAsync(
        Guid ruleSetId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ruleSet = await _ruleSetRepository.GetByIdAsync(ruleSetId, cancellationToken);
            if (ruleSet == null)
            {
                return Result.Failure("Commission rule set not found");
            }

            ruleSet.RemoveProductBinding(productId);
            await _ruleSetRepository.UpdateAsync(ruleSet);
            
            _logger.LogInformation("Removed product binding for product {ProductId} from rule set {RuleSetId}", 
                productId, ruleSetId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product binding from rule set {RuleSetId}", ruleSetId);
            return Result.Failure("Failed to remove product binding");
        }
    }

    private static string GenerateRuleSetCode(string name)
    {
        // Generate a unique code based on name and timestamp
        var cleanName = new string(name.Where(char.IsLetterOrDigit).ToArray()).ToUpper();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"{cleanName}_{timestamp}";
    }

    private static CommissionRuleDto MapToDto(CommissionRuleSet ruleSet)
    {
        return new CommissionRuleDto
        {
            Id = ruleSet.Id,
            Name = ruleSet.Name,
            Description = ruleSet.Description,
            IsActive = ruleSet.IsActive,
            IsDefault = ruleSet.IsDefault,
            MaxLevels = ruleSet.MaxLevels,
            MinimumPurchaseAmount = ruleSet.MinimumPurchaseAmount,
            RequireActiveUpline = ruleSet.RequireActiveUpline,
            CreatedOn = ruleSet.CreatedOn,
            CommissionLevels = ruleSet.CommissionLevels?.Select(MapLevelToDto).ToList() ?? new List<CommissionLevelDto>(),
            ProductBindings = ruleSet.ProductBindings?.Select(MapBindingToDto).ToList() ?? new List<ProductRuleBindingDto>()
        };
    }

    private static CommissionLevelDto MapLevelToDto(CommissionLevel level)
    {
        return new CommissionLevelDto
        {
            Id = level.Id,
            Level = level.Level,
            Percentage = level.Percentage,
            FixedAmount = level.FixedAmount,
            MaxAmount = level.MaxAmount,
            MinAmount = level.MinAmount,
            IsActive = level.IsActive
        };
    }

    private static ProductRuleBindingDto MapBindingToDto(ProductRuleBinding binding)
    {
        return new ProductRuleBindingDto
        {
            Id = binding.Id,
            ProductId = binding.ProductId,
            IsActive = binding.IsActive,
            ValidFrom = binding.ValidFrom,
            ValidTo = binding.ValidTo
        };
    }

    private static CommissionRuleVersionDto MapVersionToDto(CommissionRuleVersion version)
    {
        return new CommissionRuleVersionDto
        {
            Id = version.Id,
            RuleSetId = version.RuleSetId,
            Version = version.Version,
            SchemaJson = version.SchemaJson,
            IsPublished = version.IsPublished,
            PublishedAt = version.PublishedAt,
            PublishedBy = version.PublishedBy,
            CreatedOn = version.CreatedOn
        };
    }
}