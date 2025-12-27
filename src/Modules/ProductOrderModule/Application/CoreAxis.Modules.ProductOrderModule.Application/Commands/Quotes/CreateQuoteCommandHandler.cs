using CoreAxis.Modules.ProductOrderModule.Application.DTOs.Quotes;
using CoreAxis.Modules.ProductOrderModule.Domain.Quotes;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Context;
using CoreAxis.SharedKernel.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ExecutionContext = CoreAxis.SharedKernel.Context.ExecutionContext;

namespace CoreAxis.Modules.ProductOrderModule.Application.Commands.Quotes;

public class CreateQuoteCommandHandler : IRequestHandler<CreateQuoteCommand, Result<QuoteResponseDto>>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly IRepository<QuoteWorkflowBinding> _bindingRepository;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IUnitOfWork _unitOfWork;

    public CreateQuoteCommandHandler(
        IQuoteRepository quoteRepository,
        IRepository<QuoteWorkflowBinding> bindingRepository,
        IWorkflowRunner workflowRunner,
        IUnitOfWork unitOfWork)
    {
        _quoteRepository = quoteRepository;
        _bindingRepository = bindingRepository;
        _workflowRunner = workflowRunner;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<QuoteResponseDto>> Handle(CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        // 1. Find Binding
        var binding = await _bindingRepository.Find(b => b.AssetCode == request.AssetCode && b.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (binding == null)
        {
            return Result<QuoteResponseDto>.Failure($"No workflow binding found for asset code {request.AssetCode}");
        }

        // 2. Build Context
        var ctx = new ExecutionContext
        {
            FormRawJson = request.ApplicationData, // Must-fix 1: Do NOT deserialize to Dictionary
            Vars = new Dictionary<string, object>(),
            Steps = new Dictionary<string, StepContext>(),
            Meta = new ExecutionContextMeta {
                TraceId = Guid.NewGuid().ToString("N"),
                Trigger = "quote",
                AssetCode = request.AssetCode,
                StartedAt = DateTimeOffset.UtcNow // Should-fix 1
            }
        };

        // Try to set Form as JsonElement if valid JSON (for backward compat with other components expecting Form)
        try
        {
             ctx.Form = JsonSerializer.Deserialize<JsonElement>(request.ApplicationData);
        }
        catch
        {
             // Ignore, Form will be default object
        }

        // 3. Run Workflow
        var runResult = await _workflowRunner.RunAsync(binding.WorkflowCode, binding.WorkflowVersion, ctx, cancellationToken);

        if (!runResult.Success)
        {
            return Result<QuoteResponseDto>.Failure(runResult.ErrorMessage ?? "Workflow failed");
        }

        // 4. Map Output
        var responseDto = new QuoteResponseDto();

        if (!string.IsNullOrEmpty(runResult.OutputJson)) // Must-fix 4: Use OutputJson
        {
             try {
                responseDto = JsonSerializer.Deserialize<QuoteResponseDto>(runResult.OutputJson) ?? new QuoteResponseDto();
             } catch {}
        }
        else if (runResult.Output is Dictionary<string, object> outputDict) // Fallback
        {
             var json = JsonSerializer.Serialize(outputDict);
             try {
                responseDto = JsonSerializer.Deserialize<QuoteResponseDto>(json) ?? new QuoteResponseDto();
             } catch {}
        }
        else 
        {
             // Fallback for empty mapping / dry-run
             responseDto.FinalPremium = 0; 
             responseDto.Blocks = new List<UiBlock>();
             responseDto.RawVars = ctx.Vars; // For debug
        }

        // 5. Create Quote Entity
        var assetCode = AssetCode.Create(request.AssetCode);
        var quote = Quote.Create(
            assetCode, 
            request.ApplicationData, 
            DateTime.UtcNow.AddDays(7)); 

        quote.MarkAsReady(
            responseDto.FinalPremium, 
            JsonSerializer.Serialize(responseDto.Blocks)); 

        await _quoteRepository.AddAsync(quote);
        await _unitOfWork.SaveChangesAsync();

        responseDto.QuoteId = quote.Id;

        return Result<QuoteResponseDto>.Success(responseDto);
    }
}
