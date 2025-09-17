using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Queries.Reconciliation;

public record GetReconciliationSessionByIdQuery(Guid Id) : IRequest<ReconciliationSessionDto?>;

public class GetReconciliationSessionByIdQueryHandler : IRequestHandler<GetReconciliationSessionByIdQuery, ReconciliationSessionDto?>
{
    private readonly IReconciliationRepository _reconciliationRepository;
    private readonly ILogger<GetReconciliationSessionByIdQueryHandler> _logger;

    public GetReconciliationSessionByIdQueryHandler(
        IReconciliationRepository reconciliationRepository,
        ILogger<GetReconciliationSessionByIdQueryHandler> logger)
    {
        _reconciliationRepository = reconciliationRepository;
        _logger = logger;
    }

    public async Task<ReconciliationSessionDto?> Handle(GetReconciliationSessionByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _reconciliationRepository.GetByIdWithEntriesAsync(request.Id);
            if (session == null)
            {
                _logger.LogWarning("Reconciliation session with ID {SessionId} not found", request.Id);
                return null;
            }

            var sessionDto = new ReconciliationSessionDto
            {
                Id = session.Id,
                SessionName = session.SessionName,
                Status = session.Status,
                StartDate = session.StartDate,
                EndDate = session.EndDate,
                TotalTransactionCount = session.TotalTransactionCount,
                ReconciledTransactionCount = session.ReconciledTransactionCount,
                UnreconciledTransactionCount = session.UnreconciledTransactionCount,
                TotalAmount = session.TotalAmount,
                ReconciledAmount = session.ReconciledAmount,
                UnreconciledAmount = session.UnreconciledAmount,
                Currency = session.Currency,
                Notes = session.Notes,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                CompletedAt = session.CompletedAt,
                Entries = session.Entries?.Select(entry => new ReconciliationEntryDto
                {
                    Id = entry.Id,
                    ReconciliationSessionId = entry.ReconciliationSessionId,
                    TransactionId = entry.TransactionId,
                    TransactionType = entry.TransactionType,
                    Amount = entry.Amount,
                    Currency = entry.Currency,
                    TransactionDate = entry.TransactionDate,
                    Status = entry.Status,
                    ExternalReference = entry.ExternalReference,
                    Notes = entry.Notes,
                    ReconciledAt = entry.ReconciledAt,
                    CreatedAt = entry.CreatedAt,
                    UpdatedAt = entry.UpdatedAt
                }).ToList() ?? new List<ReconciliationEntryDto>()
            };

            _logger.LogInformation("Retrieved reconciliation session with ID: {SessionId}", request.Id);
            return sessionDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reconciliation session with ID: {SessionId}", request.Id);
            throw;
        }
    }
}