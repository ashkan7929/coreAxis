using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record DeleteWebServiceParamCommand(Guid Id) : IRequest<bool>;

public class DeleteWebServiceParamCommandHandler : IRequestHandler<DeleteWebServiceParamCommand, bool>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<DeleteWebServiceParamCommandHandler> _logger;

    public DeleteWebServiceParamCommandHandler(DbContext dbContext, ILogger<DeleteWebServiceParamCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteWebServiceParamCommand request, CancellationToken cancellationToken)
    {
        var parameter = await _dbContext.Set<WebServiceParam>()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        
        if (parameter == null)
        {
            throw new ArgumentException($"WebServiceParam with ID {request.Id} not found");
        }

        _dbContext.Set<WebServiceParam>().Remove(parameter);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted WebServiceParam {Name} with ID {Id}", 
            parameter.Name, parameter.Id);

        return true;
    }
}