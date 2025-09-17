using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record DeleteWebServiceMethodCommand(Guid Id) : IRequest<bool>;

public class DeleteWebServiceMethodCommandHandler : IRequestHandler<DeleteWebServiceMethodCommand, bool>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<DeleteWebServiceMethodCommandHandler> _logger;

    public DeleteWebServiceMethodCommandHandler(DbContext dbContext, ILogger<DeleteWebServiceMethodCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteWebServiceMethodCommand request, CancellationToken cancellationToken)
    {
        var method = await _dbContext.Set<WebServiceMethod>()
            .Include(m => m.Parameters)
            .Include(m => m.CallLogs)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        
        if (method == null)
        {
            throw new ArgumentException($"WebServiceMethod with ID {request.Id} not found");
        }

        // Remove all related entities
        _dbContext.Set<WebServiceParam>().RemoveRange(method.Parameters);
        _dbContext.Set<WebServiceCallLog>().RemoveRange(method.CallLogs);
        _dbContext.Set<WebServiceMethod>().Remove(method);
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted WebServiceMethod {HttpMethod} {Path} with ID {Id}", 
            method.HttpMethod, method.Path, method.Id);

        return true;
    }
}