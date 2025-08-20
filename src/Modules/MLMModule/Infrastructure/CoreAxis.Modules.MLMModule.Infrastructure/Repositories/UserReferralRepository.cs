using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.Modules.MLMModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.MLMModule.Infrastructure.Repositories;

public class UserReferralRepository : IUserReferralRepository
{
    private readonly MLMModuleDbContext _context;

    public UserReferralRepository(MLMModuleDbContext context)
    {
        _context = context;
    }

    public async Task<UserReferral?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserReferrals
            .Include(ur => ur.Parent)
            .Include(ur => ur.Children)
            .FirstOrDefaultAsync(ur => ur.UserId == userId, cancellationToken);
    }

    public async Task<UserReferral?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserReferrals
            .Include(ur => ur.Parent)
            .Include(ur => ur.Children)
            .FirstOrDefaultAsync(ur => ur.Id == id, cancellationToken);
    }

    public async Task<List<UserReferral>> GetChildrenAsync(Guid parentUserId, CancellationToken cancellationToken = default)
    {
        return await _context.UserReferrals
            .Where(ur => ur.ParentUserId == parentUserId)
            .OrderBy(ur => ur.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserReferral>> GetUplineAsync(Guid userId, int maxLevels = 10, CancellationToken cancellationToken = default)
    {
        var upline = new List<UserReferral>();
        var currentUserId = userId;
        var level = 0;

        while (level < maxLevels)
        {
            var userReferral = await _context.UserReferrals
                .FirstOrDefaultAsync(ur => ur.UserId == currentUserId, cancellationToken);

            if (userReferral?.ParentUserId == null)
                break;

            var parent = await _context.UserReferrals
                .FirstOrDefaultAsync(ur => ur.UserId == userReferral.ParentUserId.Value, cancellationToken);

            if (parent == null)
                break;

            upline.Add(parent);
            currentUserId = parent.UserId;
            level++;
        }

        return upline;
    }

    public async Task<List<UserReferral>> GetDownlineAsync(Guid userId, int maxLevels = 10, CancellationToken cancellationToken = default)
    {
        var userReferral = await GetByUserIdAsync(userId, cancellationToken);
        if (userReferral == null)
            return new List<UserReferral>();

        var downline = new List<UserReferral>();
        await GetDownlineRecursiveAsync(userReferral.Path, maxLevels, 0, downline, cancellationToken);
        return downline;
    }

    private async Task GetDownlineRecursiveAsync(string parentPath, int maxLevels, int currentLevel, List<UserReferral> downline, CancellationToken cancellationToken)
    {
        if (currentLevel >= maxLevels)
            return;

        var children = await _context.UserReferrals
            .Where(ur => ur.Path.StartsWith(parentPath) && ur.Path != parentPath && ur.Level == currentLevel + 1)
            .ToListAsync(cancellationToken);

        downline.AddRange(children);

        foreach (var child in children)
        {
            await GetDownlineRecursiveAsync(child.Path, maxLevels, currentLevel + 1, downline, cancellationToken);
        }
    }

    public async Task<int> GetNetworkSizeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userReferral = await GetByUserIdAsync(userId, cancellationToken);
        if (userReferral == null)
            return 0;

        return await _context.UserReferrals
            .CountAsync(ur => ur.Path.StartsWith(userReferral.Path) && ur.Id != userReferral.Id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserReferrals
            .AnyAsync(ur => ur.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(UserReferral userReferral, CancellationToken cancellationToken = default)
    {
        await _context.UserReferrals.AddAsync(userReferral, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserReferral userReferral, CancellationToken cancellationToken = default)
    {
        _context.UserReferrals.Update(userReferral);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserReferral userReferral, CancellationToken cancellationToken = default)
    {
        _context.UserReferrals.Remove(userReferral);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<UserReferral>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.UserReferrals
            .Include(ur => ur.Parent)
            .OrderBy(ur => ur.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}