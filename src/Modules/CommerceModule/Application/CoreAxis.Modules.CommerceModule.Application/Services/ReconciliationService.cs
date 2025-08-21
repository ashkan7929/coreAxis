using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Domain.Events;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.CommerceModule.Application.Services;

/// <summary>
/// Service for reconciling payment gateway statements with internal payments and orders.
/// </summary>
public class ReconciliationService : IReconciliationService
{
    private readonly ICommerceDbContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<ReconciliationService> _logger;

    public ReconciliationService(
        ICommerceDbContext context,
        IDomainEventDispatcher eventDispatcher,
        ILogger<ReconciliationService> logger)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Processes a payment gateway statement for reconciliation.
    /// </summary>
    public async Task<ReconciliationResult> ProcessStatementAsync(
        PaymentGatewayStatement statement,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Starting reconciliation for statement {StatementId} from {Provider} covering period {StartDate} to {EndDate}",
                statement.Id, statement.PaymentProvider, statement.PeriodStart, statement.PeriodEnd);

            var reconciliationSession = new ReconciliationSession
            {
                Id = Guid.NewGuid(),
                StatementId = statement.Id,
                PaymentProvider = statement.PaymentProvider,
                PeriodStart = statement.PeriodStart,
                PeriodEnd = statement.PeriodEnd,
                Status = ReconciliationStatus.InProgress,
                StartedAt = DateTime.UtcNow,
                CorrelationId = correlationId
            };

            await _context.ReconciliationSessions.AddAsync(reconciliationSession, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Process statement transactions
            var matchingResults = await ProcessStatementTransactionsAsync(
                statement, reconciliationSession, cancellationToken);

            // Generate reconciliation report
            var report = await GenerateReconciliationReportAsync(
                reconciliationSession, matchingResults, cancellationToken);

            // Update session status
            reconciliationSession.Status = ReconciliationStatus.Completed;
            reconciliationSession.CompletedAt = DateTime.UtcNow;
            reconciliationSession.TotalTransactions = statement.Transactions.Count;
            reconciliationSession.MatchedTransactions = matchingResults.Count(r => r.IsMatched);
            reconciliationSession.UnmatchedTransactions = matchingResults.Count(r => !r.IsMatched);
            reconciliationSession.TotalAmount = statement.Transactions.Sum(t => t.Amount);
            reconciliationSession.MatchedAmount = matchingResults.Where(r => r.IsMatched).Sum(r => r.StatementTransaction.Amount);
            reconciliationSession.UnmatchedAmount = matchingResults.Where(r => !r.IsMatched).Sum(r => r.StatementTransaction.Amount);

            await _context.SaveChangesAsync(cancellationToken);

            // Dispatch completion event
            await _eventDispatcher.DispatchAsync(
                new ReconciliationCompletedEvent(
                    reconciliationSession.Id,
                    statement.PaymentProvider,
                    reconciliationSession.TotalTransactions,
                    reconciliationSession.MatchedTransactions,
                    reconciliationSession.UnmatchedTransactions,
                    reconciliationSession.TotalAmount,
                    reconciliationSession.MatchedAmount,
                    reconciliationSession.UnmatchedAmount,
                    reconciliationSession.CompletedAt.Value,
                    correlationId),
                cancellationToken);

            _logger.LogInformation(
                "Completed reconciliation for statement {StatementId}. Matched: {Matched}/{Total} transactions, Amount: {MatchedAmount}/{TotalAmount}",
                statement.Id, reconciliationSession.MatchedTransactions, reconciliationSession.TotalTransactions,
                reconciliationSession.MatchedAmount, reconciliationSession.TotalAmount);

            return new ReconciliationResult
            {
                Success = true,
                SessionId = reconciliationSession.Id,
                MatchingResults = matchingResults,
                Report = report,
                Summary = new ReconciliationSummary
                {
                    TotalTransactions = reconciliationSession.TotalTransactions,
                    MatchedTransactions = reconciliationSession.MatchedTransactions,
                    UnmatchedTransactions = reconciliationSession.UnmatchedTransactions,
                    TotalAmount = reconciliationSession.TotalAmount,
                    MatchedAmount = reconciliationSession.MatchedAmount,
                    UnmatchedAmount = reconciliationSession.UnmatchedAmount,
                    MatchRate = reconciliationSession.TotalTransactions > 0 
                        ? (decimal)reconciliationSession.MatchedTransactions / reconciliationSession.TotalTransactions * 100 
                        : 0
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process reconciliation for statement {StatementId}", statement.Id);
            
            return new ReconciliationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Processes individual transactions from the statement.
    /// </summary>
    private async Task<List<TransactionMatchingResult>> ProcessStatementTransactionsAsync(
        PaymentGatewayStatement statement,
        ReconciliationSession session,
        CancellationToken cancellationToken)
    {
        var matchingResults = new List<TransactionMatchingResult>();

        foreach (var transaction in statement.Transactions)
        {
            try
            {
                var matchingResult = await MatchTransactionAsync(
                    transaction, session, cancellationToken);
                
                matchingResults.Add(matchingResult);

                // Record matching result
                var reconciliationEntry = new ReconciliationEntry
                {
                    Id = Guid.NewGuid(),
                    SessionId = session.Id,
                    StatementTransactionId = transaction.Id,
                    PaymentId = matchingResult.MatchedPayment?.Id,
                    OrderId = matchingResult.MatchedOrder?.Id,
                    MatchStatus = matchingResult.IsMatched ? MatchStatus.Matched : MatchStatus.Unmatched,
                    MatchConfidence = matchingResult.MatchConfidence,
                    MatchingCriteria = string.Join(", ", matchingResult.MatchingCriteria),
                    AmountDifference = matchingResult.AmountDifference,
                    TimeDifference = matchingResult.TimeDifference,
                    Notes = matchingResult.Notes,
                    ProcessedAt = DateTime.UtcNow
                };

                await _context.ReconciliationEntries.AddAsync(reconciliationEntry, cancellationToken);

                // Dispatch events based on matching result
                if (matchingResult.IsMatched)
                {
                    await _eventDispatcher.DispatchAsync(
                        new TransactionMatchedEvent(
                            session.Id,
                            transaction.Id,
                            matchingResult.MatchedPayment?.Id,
                            matchingResult.MatchedOrder?.Id,
                            transaction.Amount,
                            matchingResult.MatchConfidence,
                            DateTime.UtcNow,
                            null),
                        cancellationToken);
                }
                else
                {
                    await _eventDispatcher.DispatchAsync(
                        new TransactionUnmatchedEvent(
                            session.Id,
                            transaction.Id,
                            transaction.Amount,
                            transaction.TransactionDate,
                            matchingResult.UnmatchedReason ?? "No matching payment found",
                            DateTime.UtcNow,
                            null),
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to process transaction {TransactionId} in session {SessionId}", 
                    transaction.Id, session.Id);

                matchingResults.Add(new TransactionMatchingResult
                {
                    StatementTransaction = transaction,
                    IsMatched = false,
                    UnmatchedReason = $"Processing error: {ex.Message}",
                    MatchConfidence = 0
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return matchingResults;
    }

    /// <summary>
    /// Attempts to match a statement transaction with internal payments.
    /// </summary>
    private async Task<TransactionMatchingResult> MatchTransactionAsync(
        StatementTransaction transaction,
        ReconciliationSession session,
        CancellationToken cancellationToken)
    {
        var matchingCriteria = new List<string>();
        var potentialMatches = new List<PaymentMatchCandidate>();

        // Strategy 1: Exact match by external transaction ID
        if (!string.IsNullOrEmpty(transaction.ExternalTransactionId))
        {
            var exactMatch = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => 
                    p.TransactionId == transaction.ExternalTransactionId,
                    cancellationToken);

            if (exactMatch != null)
            {
                return new TransactionMatchingResult
                {
                    StatementTransaction = transaction,
                    MatchedPayment = exactMatch,
                    MatchedOrder = exactMatch.Order,
                    IsMatched = true,
                    MatchConfidence = 100,
                    MatchingCriteria = new[] { "ExternalTransactionId" },
                    AmountDifference = Math.Abs(transaction.Amount - exactMatch.Amount),
                    TimeDifference = Math.Abs((transaction.TransactionDate - exactMatch.CreatedOn).TotalMinutes)
                };
            }
        }

        // Strategy 2: Match by amount and time window
        var timeWindow = TimeSpan.FromHours(24); // Configurable
        var amountTolerance = 0.01m; // Configurable

        var amountMatches = await _context.Payments
            .Include(p => p.Order)
            .Where(p => 
                Math.Abs(p.Amount - transaction.Amount) <= amountTolerance &&
                p.CreatedOn >= transaction.TransactionDate.Subtract(timeWindow) &&
                    p.CreatedOn <= transaction.TransactionDate.Add(timeWindow) &&
                p.Status == PaymentStatus.Completed)
            .ToListAsync(cancellationToken);

        foreach (var payment in amountMatches)
        {
            var confidence = CalculateMatchConfidence(transaction, payment);
            potentialMatches.Add(new PaymentMatchCandidate
            {
                Payment = payment,
                Confidence = confidence,
                AmountDifference = Math.Abs(transaction.Amount - payment.Amount),
                TimeDifference = Math.Abs((transaction.TransactionDate - payment.CreatedOn).TotalMinutes)
            });
        }

        // Strategy 3: Match by reference number or order ID
        if (!string.IsNullOrEmpty(transaction.ReferenceNumber))
        {
            var referenceMatches = await _context.Payments
                .Include(p => p.Order)
                .Where(p => 
                    (p.GatewayReference == transaction.ReferenceNumber ||
                     p.Order.OrderNumber == transaction.ReferenceNumber) &&
                    p.CreatedOn >= session.PeriodStart &&
                    p.CreatedOn <= session.PeriodEnd)
                .ToListAsync(cancellationToken);

            foreach (var payment in referenceMatches)
            {
                var confidence = CalculateMatchConfidence(transaction, payment);
                var existing = potentialMatches.FirstOrDefault(m => m.Payment.Id == payment.Id);
                if (existing != null)
                {
                    existing.Confidence = Math.Max(existing.Confidence, confidence + 20); // Boost for reference match
                }
                else
                {
                    potentialMatches.Add(new PaymentMatchCandidate
                    {
                        Payment = payment,
                        Confidence = confidence + 20,
                        AmountDifference = Math.Abs(transaction.Amount - payment.Amount),
                        TimeDifference = Math.Abs((transaction.TransactionDate - payment.CreatedOn).TotalMinutes)
                    });
                }
            }
        }

        // Select best match
        var bestMatch = potentialMatches
            .Where(m => m.Confidence >= 70) // Minimum confidence threshold
            .OrderByDescending(m => m.Confidence)
            .ThenBy(m => m.AmountDifference)
            .ThenBy(m => m.TimeDifference)
            .FirstOrDefault();

        if (bestMatch != null)
        {
            var criteria = new List<string>();
            if (bestMatch.AmountDifference <= amountTolerance) criteria.Add("Amount");
            if (bestMatch.TimeDifference <= 60) criteria.Add("Time");
            if (!string.IsNullOrEmpty(transaction.ReferenceNumber)) criteria.Add("Reference");

            return new TransactionMatchingResult
            {
                StatementTransaction = transaction,
                MatchedPayment = bestMatch.Payment,
                MatchedOrder = bestMatch.Payment.Order,
                IsMatched = true,
                MatchConfidence = bestMatch.Confidence,
                MatchingCriteria = criteria,
                AmountDifference = bestMatch.AmountDifference,
                TimeDifference = bestMatch.TimeDifference
            };
        }

        // No match found
        return new TransactionMatchingResult
        {
            StatementTransaction = transaction,
            IsMatched = false,
            UnmatchedReason = DetermineUnmatchedReason(transaction, potentialMatches),
            MatchConfidence = 0
        };
    }

    /// <summary>
    /// Calculates match confidence score between statement transaction and payment.
    /// </summary>
    private int CalculateMatchConfidence(StatementTransaction transaction, Payment payment)
    {
        var confidence = 0;

        // Amount match (40 points)
        var amountDiff = Math.Abs(transaction.Amount - payment.Amount);
        if (amountDiff == 0) confidence += 40;
        else if (amountDiff <= 0.01m) confidence += 35;
        else if (amountDiff <= 0.10m) confidence += 25;
        else if (amountDiff <= 1.00m) confidence += 15;

        // Time proximity (30 points)
        var timeDiff = Math.Abs((transaction.TransactionDate - payment.CreatedOn).TotalHours);
        if (timeDiff <= 1) confidence += 30;
        else if (timeDiff <= 6) confidence += 25;
        else if (timeDiff <= 24) confidence += 20;
        else if (timeDiff <= 72) confidence += 10;

        // Reference match (20 points)
        if (!string.IsNullOrEmpty(transaction.ReferenceNumber) &&
            (payment.ReferenceNumber == transaction.ReferenceNumber ||
             payment.Order?.OrderNumber == transaction.ReferenceNumber))
        {
            confidence += 20;
        }

        // Currency match (10 points)
        if (transaction.Currency == payment.Currency)
        {
            confidence += 10;
        }

        return Math.Min(confidence, 100);
    }

    /// <summary>
    /// Determines the reason why a transaction couldn't be matched.
    /// </summary>
    private string DetermineUnmatchedReason(StatementTransaction transaction, List<PaymentMatchCandidate> candidates)
    {
        if (!candidates.Any())
        {
            return "No potential matches found in the system";
        }

        var bestCandidate = candidates.OrderByDescending(c => c.Confidence).First();
        
        if (bestCandidate.AmountDifference > 1.00m)
        {
            return $"Amount mismatch: Statement {transaction.Amount:C}, System {bestCandidate.Payment.Amount:C}";
        }

        if (bestCandidate.TimeDifference > 72 * 60) // 72 hours in minutes
        {
            return $"Time difference too large: {bestCandidate.TimeDifference / 60:F1} hours";
        }

        return $"Low confidence match: {bestCandidate.Confidence}%";
    }

    /// <summary>
    /// Generates a comprehensive reconciliation report.
    /// </summary>
    private async Task<ReconciliationReport> GenerateReconciliationReportAsync(
        ReconciliationSession session,
        List<TransactionMatchingResult> matchingResults,
        CancellationToken cancellationToken)
    {
        var report = new ReconciliationReport
        {
            SessionId = session.Id,
            PaymentProvider = session.PaymentProvider,
            PeriodStart = session.PeriodStart,
            PeriodEnd = session.PeriodEnd,
            GeneratedAt = DateTime.UtcNow
        };

        // Summary statistics
        report.Summary = new ReconciliationSummary
        {
            TotalTransactions = matchingResults.Count,
            MatchedTransactions = matchingResults.Count(r => r.IsMatched),
            UnmatchedTransactions = matchingResults.Count(r => !r.IsMatched),
            TotalAmount = matchingResults.Sum(r => r.StatementTransaction.Amount),
            MatchedAmount = matchingResults.Where(r => r.IsMatched).Sum(r => r.StatementTransaction.Amount),
            UnmatchedAmount = matchingResults.Where(r => !r.IsMatched).Sum(r => r.StatementTransaction.Amount),
            MatchRate = matchingResults.Count > 0 
                ? (decimal)matchingResults.Count(r => r.IsMatched) / matchingResults.Count * 100 
                : 0
        };

        // Unmatched transactions details
        report.UnmatchedTransactions = matchingResults
            .Where(r => !r.IsMatched)
            .Select(r => new UnmatchedTransactionInfo
            {
                TransactionId = r.StatementTransaction.Id,
                Amount = r.StatementTransaction.Amount,
                Currency = r.StatementTransaction.Currency,
                TransactionDate = r.StatementTransaction.TransactionDate,
                ReferenceNumber = r.StatementTransaction.ReferenceNumber,
                Reason = r.UnmatchedReason ?? "Unknown"
            })
            .ToList();

        // Amount discrepancies
        report.AmountDiscrepancies = matchingResults
            .Where(r => r.IsMatched && r.AmountDifference > 0.01m)
            .Select(r => new AmountDiscrepancyInfo
            {
                TransactionId = r.StatementTransaction.Id,
                PaymentId = r.MatchedPayment!.Id,
                StatementAmount = r.StatementTransaction.Amount,
                SystemAmount = r.MatchedPayment.Amount,
                Difference = r.AmountDifference
            })
            .ToList();

        return report;
    }

    /// <summary>
    /// Retrieves reconciliation history for a specific period.
    /// </summary>
    public async Task<List<ReconciliationSession>> GetReconciliationHistoryAsync(
        string? paymentProvider = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReconciliationSessions.AsQueryable();

        if (!string.IsNullOrEmpty(paymentProvider))
        {
            query = query.Where(s => s.PaymentProvider == paymentProvider);
        }

        if (startDate.HasValue)
        {
            query = query.Where(s => s.PeriodStart >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.PeriodEnd <= endDate.Value);
        }

        return await query
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets detailed reconciliation results for a specific session.
    /// </summary>
    public async Task<ReconciliationSessionDetails> GetSessionDetailsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.ReconciliationSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            throw new InvalidOperationException($"Reconciliation session {sessionId} not found");
        }

        var entries = await _context.ReconciliationEntries
            .Include(e => e.Payment)
            .ThenInclude(p => p!.Order)
            .Where(e => e.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        return new ReconciliationSessionDetails
        {
            Session = session,
            Entries = entries
        };
    }
}

/// <summary>
/// Interface for the reconciliation service.
/// </summary>
public interface IReconciliationService
{
    Task<ReconciliationResult> ProcessStatementAsync(
        PaymentGatewayStatement statement,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
    
    Task<List<ReconciliationSession>> GetReconciliationHistoryAsync(
        string? paymentProvider = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
    
    Task<ReconciliationSessionDetails> GetSessionDetailsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a reconciliation process.
/// </summary>
public class ReconciliationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? SessionId { get; set; }
    public List<TransactionMatchingResult> MatchingResults { get; set; } = new();
    public ReconciliationReport? Report { get; set; }
    public ReconciliationSummary? Summary { get; set; }
}

/// <summary>
/// Result of matching a single transaction.
/// </summary>
public class TransactionMatchingResult
{
    public StatementTransaction StatementTransaction { get; set; } = null!;
    public Payment? MatchedPayment { get; set; }
    public Order? MatchedOrder { get; set; }
    public bool IsMatched { get; set; }
    public int MatchConfidence { get; set; }
    public IEnumerable<string> MatchingCriteria { get; set; } = new List<string>();
    public decimal AmountDifference { get; set; }
    public double TimeDifference { get; set; }
    public string? UnmatchedReason { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Candidate payment for matching.
/// </summary>
public class PaymentMatchCandidate
{
    public Payment Payment { get; set; } = null!;
    public int Confidence { get; set; }
    public decimal AmountDifference { get; set; }
    public double TimeDifference { get; set; }
}

/// <summary>
/// Comprehensive reconciliation report.
/// </summary>
public class ReconciliationReport
{
    public Guid SessionId { get; set; }
    public string PaymentProvider { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime GeneratedAt { get; set; }
    public ReconciliationSummary Summary { get; set; } = null!;
    public List<UnmatchedTransactionInfo> UnmatchedTransactions { get; set; } = new();
    public List<AmountDiscrepancyInfo> AmountDiscrepancies { get; set; } = new();
}

/// <summary>
/// Summary statistics for reconciliation.
/// </summary>
public class ReconciliationSummary
{
    public int TotalTransactions { get; set; }
    public int MatchedTransactions { get; set; }
    public int UnmatchedTransactions { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal MatchedAmount { get; set; }
    public decimal UnmatchedAmount { get; set; }
    public decimal MatchRate { get; set; }
}

/// <summary>
/// Information about unmatched transactions.
/// </summary>
public class UnmatchedTransactionInfo
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Information about amount discrepancies.
/// </summary>
public class AmountDiscrepancyInfo
{
    public string TransactionId { get; set; } = string.Empty;
    public Guid PaymentId { get; set; }
    public decimal StatementAmount { get; set; }
    public decimal SystemAmount { get; set; }
    public decimal Difference { get; set; }
}

/// <summary>
/// Detailed reconciliation session information.
/// </summary>
public class ReconciliationSessionDetails
{
    public ReconciliationSession Session { get; set; } = null!;
    public List<ReconciliationEntry> Entries { get; set; } = new();
}

/// <summary>
/// Payment gateway statement for reconciliation.
/// </summary>
public class PaymentGatewayStatement
{
    public string Id { get; set; } = string.Empty;
    public string PaymentProvider { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<StatementTransaction> Transactions { get; set; } = new();
    public string? MetadataJson { get; set; }
}

/// <summary>
/// Individual transaction from payment gateway statement.
/// </summary>
public class StatementTransaction
{
    public string Id { get; set; } = string.Empty;
    public string? ExternalTransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
}