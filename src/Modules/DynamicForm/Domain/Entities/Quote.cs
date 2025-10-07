using CoreAxis.SharedKernel;
using System;

namespace CoreAxis.Modules.DynamicForm.Domain.Entities
{
    /// <summary>
    /// Represents a persisted quote with snapshots and TTL/consumed flags.
    /// </summary>
    public class Quote : EntityBase
    {
        public Guid ProductId { get; set; }

        /// <summary>
        /// Absolute expiration timestamp (UTC).
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Indicates whether the quote has been consumed.
        /// </summary>
        public bool Consumed { get; set; } = false;

        /// <summary>
        /// JSON payload of PricingResultDto.
        /// </summary>
        public string PricingJson { get; set; } = string.Empty;

        /// <summary>
        /// JSON payload of inputs snapshot.
        /// </summary>
        public string InputsSnapshotJson { get; set; } = string.Empty;

        /// <summary>
        /// JSON payload of external data snapshot.
        /// </summary>
        public string ExternalDataSnapshotJson { get; set; } = string.Empty;
    }
}