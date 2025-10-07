using System;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.DynamicForm.Domain.Entities
{
    /// <summary>
    /// Represents an external data source used by pricing formulas.
    /// </summary>
    public class DataSource : EntityBase
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string ServiceName { get; private set; } = string.Empty;
        public string EndpointName { get; private set; } = string.Empty;
        public int CacheTtlSeconds { get; private set; }
        public bool Enabled { get; private set; } = true;

        private DataSource() { }

        public DataSource(Guid id, string name, string serviceName, string endpointName, int cacheTtlSeconds, bool enabled = true)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            EndpointName = endpointName ?? throw new ArgumentNullException(nameof(endpointName));
            CacheTtlSeconds = cacheTtlSeconds;
            Enabled = enabled;
        }

        public void Update(string name, string serviceName, string endpointName, int cacheTtlSeconds, bool enabled)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            EndpointName = endpointName ?? throw new ArgumentNullException(nameof(endpointName));
            CacheTtlSeconds = cacheTtlSeconds;
            Enabled = enabled;
        }
    }
}