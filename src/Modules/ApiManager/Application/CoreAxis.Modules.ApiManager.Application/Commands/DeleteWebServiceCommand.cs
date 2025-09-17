using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record DeleteWebServiceCommand(Guid Id) : IRequest<bool>;

public class DeleteWebServiceCommandHandler : IRequestHandler<DeleteWebServiceCommand, bool>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<DeleteWebServiceCommandHandler> _logger;

    public DeleteWebServiceCommandHandler(DbContext dbContext, ILogger<DeleteWebServiceCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteWebServiceCommand request, CancellationToken cancellationToken)
    {
        var webService = await _dbContext.Set<WebService>()
            .Include(ws => ws.Methods)
            .ThenInclude(m => m.Parameters)
            .Include(ws => ws.CallLogs)
            .FirstOrDefaultAsync(ws => ws.Id == request.Id, cancellationToken);
        
        if (webService == null)
        {
            throw new ArgumentException($"WebService with ID {request.Id} not found");
        }

        // Remove all related entities
        foreach (var method in webService.Methods)
        {
            _dbContext.Set<WebServiceParam>().RemoveRange(method.Parameters);
        }
        
        _dbContext.Set<WebServiceMethod>().RemoveRange(webService.Methods);
        _dbContext.Set<WebServiceCallLog>().RemoveRange(webService.CallLogs);
        _dbContext.Set<WebService>().Remove(webService);
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted WebService {Name} with ID {Id}", webService.Name, webService.Id);

        return true;
    }
}