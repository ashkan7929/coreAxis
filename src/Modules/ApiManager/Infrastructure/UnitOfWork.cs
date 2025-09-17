using CoreAxis.Modules.ApiManager.Infrastructure.Repositories;
using CoreAxis.Shared.Abstractions.Repositories;

namespace CoreAxis.Modules.ApiManager.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApiManagerDbContext _context;
    private bool _disposed = false;

    public UnitOfWork(ApiManagerDbContext context)
    {
        _context = context;
        WebServices = new WebServiceRepository(_context);
        WebServiceMethods = new WebServiceMethodRepository(_context);
        WebServiceCallLogs = new WebServiceCallLogRepository(_context);
        SecurityProfiles = new SecurityProfileRepository(_context);
    }

    public IWebServiceRepository WebServices { get; }
    public IWebServiceMethodRepository WebServiceMethods { get; }
    public IWebServiceCallLogRepository WebServiceCallLogs { get; }
    public ISecurityProfileRepository SecurityProfiles { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CommitTransactionAsync(cancellationToken);
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.RollbackTransactionAsync(cancellationToken);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _context.DisposeAsync();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}