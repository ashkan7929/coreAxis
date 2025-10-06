using System;

namespace CoreAxis.Modules.ApiManager.Application.Security;

public interface ITimestampProvider
{
    DateTimeOffset UtcNow();
}

public sealed class SystemTimestampProvider : ITimestampProvider
{
    public DateTimeOffset UtcNow() => DateTimeOffset.UtcNow;
}