namespace CoreAxis.Modules.ProductOrderModule.Domain.Orders.ValueObjects;

public enum OrderStatus
{
    Pending = 0,
    PriceLocked = 1,
    Completed = 2,
    Cancelled = 3,
    Failed = 4
}