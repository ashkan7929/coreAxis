using System;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    public class RoundingPolicy : IRoundingPolicy
    {
        public decimal NormalizeMoney(decimal value, int precision = 2, RoundingMode mode = RoundingMode.Bankers)
        {
            var midpoint = mode == RoundingMode.Bankers ? MidpointRounding.ToEven : MidpointRounding.AwayFromZero;
            return Math.Round(value, precision, midpoint);
        }
    }
}