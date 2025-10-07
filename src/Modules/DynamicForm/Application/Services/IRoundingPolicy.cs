using System;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    public enum RoundingMode
    {
        Bankers,
        HalfUp
    }

    public interface IRoundingPolicy
    {
        decimal NormalizeMoney(decimal value, int precision = 2, RoundingMode mode = RoundingMode.Bankers);
    }
}