using System;

namespace CoreAxis.SharedKernel.Observability;

public interface ICorrelationIdAccessor
{
    Guid GetCorrelationId();
}
