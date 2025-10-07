using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    public class QuoteService : IQuoteService
    {
        private readonly DynamicFormDbContext _db;
        private static readonly TimeSpan QuoteTtl = TimeSpan.FromMinutes(20);

        public QuoteService(DynamicFormDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<QuoteResponseDto> CreateQuoteAsync(PricingCalculationRequest request, PricingResultDto pricing, Dictionary<string, object?> externalDataSnapshot, CancellationToken cancellationToken = default)
        {
            var quoteId = Guid.NewGuid();
            var expires = DateTime.UtcNow.Add(QuoteTtl);

            var entity = new Quote
            {
                Id = quoteId,
                ProductId = request.ProductId,
                ExpiresAt = expires,
                Consumed = false,
                PricingJson = JsonSerializer.Serialize(pricing),
                InputsSnapshotJson = JsonSerializer.Serialize(request.FormData),
                ExternalDataSnapshotJson = JsonSerializer.Serialize(externalDataSnapshot)
            };

            await _db.Quotes.AddAsync(entity, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return new QuoteResponseDto
            {
                QuoteId = quoteId,
                ProductId = request.ProductId,
                ExpiresAt = expires,
                Pricing = pricing,
                InputsSnapshot = request.FormData,
                ExternalDataSnapshot = externalDataSnapshot
            };
        }

        public async Task<QuoteResponseDto?> GetQuoteAsync(Guid quoteId, CancellationToken cancellationToken = default)
        {
            var (quote, isExpired, isConsumed) = await GetQuoteWithStateAsync(quoteId, cancellationToken);
            if (isExpired || isConsumed)
                return null;
            return quote;
        }

        public async Task<(QuoteResponseDto? Quote, bool IsExpired, bool IsConsumed)> GetQuoteWithStateAsync(Guid quoteId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Quotes.AsNoTracking().FirstOrDefaultAsync(q => q.Id == quoteId, cancellationToken);
            if (entity is null)
                return (null, false, false);

            var expired = DateTime.UtcNow >= entity.ExpiresAt;
            var consumed = entity.Consumed;
            if (expired || consumed)
                return (null, expired, consumed);

            var pricing = JsonSerializer.Deserialize<PricingResultDto>(entity.PricingJson) ?? new PricingResultDto();
            var inputs = JsonSerializer.Deserialize<Dictionary<string, object?>>(entity.InputsSnapshotJson) ?? new Dictionary<string, object?>();
            var external = JsonSerializer.Deserialize<Dictionary<string, object?>>(entity.ExternalDataSnapshotJson) ?? new Dictionary<string, object?>();

            var dto = new QuoteResponseDto
            {
                QuoteId = entity.Id,
                ProductId = entity.ProductId,
                ExpiresAt = entity.ExpiresAt,
                Pricing = pricing,
                InputsSnapshot = inputs,
                ExternalDataSnapshot = external
            };

            return (dto, false, false);
        }

        public async Task MarkConsumedAsync(Guid quoteId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Quotes.FirstOrDefaultAsync(q => q.Id == quoteId, cancellationToken);
            if (entity is null)
                return;
            if (!entity.Consumed)
            {
                entity.Consumed = true;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}