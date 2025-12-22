using CoreAxis.SharedKernel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Domain.Interfaces
{
    /// <summary>
    /// Coordinates fetching and caching of external data for formula/expression evaluation.
    /// Provides a simple API to retrieve values from external APIs with TTL caching.
    /// </summary>
    public interface IDataOrchestrator
    {
        /// <summary>
        /// Retrieves a set of external data values based on hints present in the context.
        /// Expected convention: context["externalDataSources"] is a dictionary mapping keys to
        /// an object containing at minimum "methodId" (Guid or string) and optional "parameters".
        /// </summary>
        /// <param name="context">Evaluation context that may include external data hints.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the dictionary of external data and trace info.</returns>
        Task<Result<ExternalDataBatchResult>> GetExternalDataAsync(
            Dictionary<string, object?> context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a single value from an external API method, with TTL cache.
        /// </summary>
        /// <param name="webServiceMethodId">API Manager method identifier.</param>
        /// <param name="parameters">Parameters to pass to the API method.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the parsed response object (JSON or string).</returns>
        Task<Result<object?>> GetValueAsync(
            Guid webServiceMethodId,
            Dictionary<string, object?> parameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears cached entries that match the given prefix (optional maintenance hook).
        /// </summary>
        /// <param name="cacheKeyPrefix">Prefix to match cache keys.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ClearCacheAsync(string cacheKeyPrefix, CancellationToken cancellationToken = default);
    }

    public class ExternalDataBatchResult
    {
        public Dictionary<string, object?> Data { get; set; } = new();
        public Dictionary<string, string> Trace { get; set; } = new();
    }
}