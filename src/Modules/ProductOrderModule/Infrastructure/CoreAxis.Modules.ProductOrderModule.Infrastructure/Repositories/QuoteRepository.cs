using CoreAxis.Modules.ProductOrderModule.Domain.Quotes;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Data;
using CoreAxis.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Repositories;

public class QuoteRepository : Repository<Quote>, IQuoteRepository
{
    public QuoteRepository(ProductOrderDbContext context) : base(context)
    {
    }

    public async Task<Quote?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(q => q.Id == id);
    }
}
