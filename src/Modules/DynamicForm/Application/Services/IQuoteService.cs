using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Application.DTOs;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    public interface IQuoteService
    {
        Task<QuoteResponseDto> CreateQuoteAsync(PricingCalculationRequest request, PricingResultDto pricing, Dictionary<string, object?> externalDataSnapshot, CancellationToken cancellationToken = default);
        Task<QuoteResponseDto?> GetQuoteAsync(Guid quoteId, CancellationToken cancellationToken = default);
        Task<(QuoteResponseDto? Quote, bool IsExpired, bool IsConsumed)> GetQuoteWithStateAsync(Guid quoteId, CancellationToken cancellationToken = default);
        Task MarkConsumedAsync(Guid quoteId, CancellationToken cancellationToken = default);
    }
}