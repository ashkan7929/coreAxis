using CoreAxis.Modules.MLMModule.Application.Contracts;
using System;
using System.Threading.Tasks;

namespace CoreAxis.Modules.MLMModule.Infrastructure.Data;

public class UnitOfWork : IMLMUnitOfWork
{
    private readonly MLMModuleDbContext _context;

    public UnitOfWork(MLMModuleDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        await _context.Database.CommitTransactionAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        await _context.Database.RollbackTransactionAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
