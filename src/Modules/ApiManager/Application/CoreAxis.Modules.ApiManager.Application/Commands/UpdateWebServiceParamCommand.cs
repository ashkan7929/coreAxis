using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record UpdateWebServiceParamCommand(
    Guid Id,
    string Name,
    ParameterLocation Location,
    string Type,
    bool IsRequired = false,
    string? DefaultValue = null
) : IRequest<bool>;

public class UpdateWebServiceParamCommandHandler : IRequestHandler<UpdateWebServiceParamCommand, bool>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<UpdateWebServiceParamCommandHandler> _logger;

    public UpdateWebServiceParamCommandHandler(DbContext dbContext, ILogger<UpdateWebServiceParamCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateWebServiceParamCommand request, CancellationToken cancellationToken)
    {
        var parameter = await _dbContext.Set<WebServiceParam>()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        
        if (parameter == null)
        {
            throw new ArgumentException($"WebServiceParam with ID {request.Id} not found");
        }

        // Check for duplicate parameter name within the same method (excluding current parameter)
        var existingParam = await _dbContext.Set<WebServiceParam>()
            .AnyAsync(p => p.MethodId == parameter.MethodId && 
                          p.Name == request.Name && 
                          p.Id != request.Id, cancellationToken);
        
        if (existingParam)
        {
            throw new InvalidOperationException($"Parameter with name '{request.Name}' already exists for this method");
        }

        parameter.Update(
            request.Name,
            request.Location,
            request.Type,
            request.IsRequired,
            request.DefaultValue
        );
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated WebServiceParam {Name} with ID {Id}", 
            request.Name, parameter.Id);

        return true;
    }
}