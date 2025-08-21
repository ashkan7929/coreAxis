using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;

namespace CoreAxis.Modules.CommerceModule.Application.Interfaces;

/// <summary>
/// Interface for the reservation service.
/// </summary>
public interface IReservationService
{
    Task<ReservationResult> CreateReservationsAsync(
        List<ReservationRequest> requests,
        Guid orderId,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ConfirmReservationsAsync(
        Guid orderId,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ReleaseReservationsAsync(
        Guid orderId,
        string? reason = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<int> CleanupExpiredReservationsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request for creating an inventory reservation.
/// </summary>
public record ReservationRequest(
    Guid ProductId,
    Guid? VariantId,
    int Quantity,
    int? ExpirationMinutes = null
);

/// <summary>
/// Result of a reservation operation.
/// </summary>
public class ReservationResult
{
    public bool Success { get; init; }
    public List<InventoryReservation> Reservations { get; init; } = new();
    public List<ReservationFailure> Failures { get; init; } = new();

    public static ReservationResult CreateSuccess(List<InventoryReservation> reservations) =>
        new() { Success = true, Reservations = reservations };

    public static ReservationResult CreateFailed(List<ReservationFailure> failures) =>
        new() { Success = false, Failures = failures };
}

/// <summary>
/// Information about a failed reservation.
/// </summary>
public record ReservationFailure(
    Guid ProductId,
    Guid? VariantId,
    int Quantity,
    string ErrorMessage
);

/// <summary>
/// Result of a single item reservation attempt.
/// </summary>
public class SingleReservationResult
{
    public bool Success { get; init; }
    public InventoryReservation? Reservation { get; init; }
    public string? ErrorMessage { get; init; }

    public static SingleReservationResult CreateSuccess(InventoryReservation reservation) =>
        new() { Success = true, Reservation = reservation };

    public static SingleReservationResult CreateFailed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}