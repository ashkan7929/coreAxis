namespace CoreAxis.Modules.MLMModule.Application.Contracts;

public class RejectCommissionRequest
{
    public Guid CommissionId { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public string? RejectionNotes { get; set; }
}