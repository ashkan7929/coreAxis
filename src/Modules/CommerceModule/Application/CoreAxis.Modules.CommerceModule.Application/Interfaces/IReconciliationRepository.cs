using CoreAxis.Modules.CommerceModule.Domain.Entities;

namespace CoreAxis.Modules.CommerceModule.Application.Interfaces;

public interface IReconciliationRepository
{
    Task<ReconciliationSession?> GetByIdAsync(Guid id);
    Task<ReconciliationSession?> GetByIdWithEntriesAsync(Guid id);
    Task<(List<ReconciliationSession> Sessions, int TotalCount)> GetReconciliationSessionsAsync(
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<ReconciliationSession> AddAsync(ReconciliationSession session);
    Task<ReconciliationSession> UpdateAsync(ReconciliationSession session);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<List<ReconciliationSession>> GetActiveSessionsAsync();
    Task<ReconciliationSession?> GetLatestSessionAsync();
}

public interface IReconciliationEntryRepository
{
    Task<ReconciliationEntry?> GetByIdAsync(Guid id);
    Task<ReconciliationEntry?> GetByTransactionIdAsync(string transactionId);
    Task<List<ReconciliationEntry>> GetEntriesBySessionIdAsync(Guid sessionId);
    Task<(List<ReconciliationEntry> Entries, int TotalCount)> GetReconciliationEntriesAsync(
        Guid? sessionId = null,
        string? status = null,
        string? transactionType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<ReconciliationEntry> AddAsync(ReconciliationEntry entry);
    Task<ReconciliationEntry> UpdateAsync(ReconciliationEntry entry);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<List<ReconciliationEntry>> GetUnreconciledEntriesAsync(Guid sessionId);
    Task<int> GetUnreconciledCountAsync(Guid sessionId);
    Task<decimal> GetUnreconciledAmountAsync(Guid sessionId);
}