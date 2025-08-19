using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.Modules.MLMModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.MLMModule.Infrastructure.Repositories;

public class CommissionRuleSetRepository : ICommissionRuleSetRepository
{
    private readonly MLMDbContext _context;

    public CommissionRuleSetRepository(MLMDbContext context)
    {
        _context = context;
    }

    public async Task<CommissionRuleSet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionRuleSets
            .Include(crs => crs.Versions)
            .Include(crs => crs.CommissionLevels)
            .Include(crs => crs.ProductBindings)
            .FirstOrDefaultAsync(crs => crs.Id == id, cancellationToken);
    }

    public async Task<CommissionRuleSet?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CommissionRuleSets
            .Include(crs => crs.Versions)
            .Include(crs => crs.CommissionLevels)
            .Include(crs => crs.ProductBindings)
            .FirstOrDefaultAsync(crs => crs.IsDefault, cancellationToken);
    }

    public async Task<CommissionRuleSet?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // First try to find a specific rule for this product
        var specificRule = await _context.CommissionRuleSets
            .Include(crs => crs.Versions)
            .Include(crs => crs.CommissionLevels)
            .Include(crs => crs.ProductBindings)
            .FirstOrDefaultAsync(crs => crs.ProductBindings.Any(prb => prb.ProductId == productId), cancellationToken);

        if (specificRule != null)
            return specificRule;

        // If no specific rule found, return the default rule
        return await GetDefaultAsync(cancellationToken);
    }

    public async Task<List<CommissionRuleSet>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CommissionRuleSets
            .Include(crs => crs.Versions)
            .Include(crs => crs.CommissionLevels)
            .Include(crs => crs.ProductBindings)
            .Where(crs => crs.IsActive)
            .OrderBy(crs => crs.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CommissionRuleSet>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionRuleSets
            .Include(crs => crs.Versions)
            .Include(crs => crs.CommissionLevels)
            .Include(crs => crs.ProductBindings)
            .OrderBy(crs => crs.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionRuleSets
            .AnyAsync(crs => crs.Id == id, cancellationToken);
    }

    public async Task AddAsync(CommissionRuleSet commissionRuleSet, CancellationToken cancellationToken = default)
    {
        await _context.CommissionRuleSets.AddAsync(commissionRuleSet, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CommissionRuleSet commissionRuleSet, CancellationToken cancellationToken = default)
    {
        _context.CommissionRuleSets.Update(commissionRuleSet);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(CommissionRuleSet commissionRuleSet, CancellationToken cancellationToken = default)
    {
        _context.CommissionRuleSets.Remove(commissionRuleSet);
        await _context.SaveChangesAsync(cancellationToken);
    }
}