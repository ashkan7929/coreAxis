using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Queries.Reconciliation;

public record GetReconciliationSessionsQuery(
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<(List<ReconciliationSessionDto> Sessions, int TotalCount)>;

public class GetReconciliationSessionsQueryHandler : IRequestHandler<GetReconciliationSessionsQuery, (List<ReconciliationSessionDto> Sessions, int TotalCount)>
{
    private readonly IReconciliationRepository _reconciliationRepository;
    private readonly ILogger<GetReconciliationSessionsQueryHandler> _logger;

    public GetReconciliationSessionsQueryHandler(
        IReconciliationRepository reconciliationRepository,
        ILogger<GetReconciliationSessionsQueryHandler> logger)
    {
        _reconciliationRepository = reconciliationRepository;
        _logger = logger;
    }

    public async Task<(List<ReconciliationSessionDto> Sessions, int TotalCount)> Handle(GetReconciliationSessionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (sessions, totalCount) = await _reconciliationRepository.GetReconciliationSessionsAsync(
                request.Status,
                request.FromDate,
                request.ToDate,
                request.PageNumber,
                request.PageSize);

            var sessionDtos = sessions.Select(session => new ReconciliationSessionDto
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
            }).ToList();

            _logger.LogInformation("Retrieved {Count} reconciliation sessions out of {TotalCount} total sessions", 
                sessionDtos.Count, totalCount);

            return (sessionDtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reconciliation sessions");
            throw;
        }
    }
}