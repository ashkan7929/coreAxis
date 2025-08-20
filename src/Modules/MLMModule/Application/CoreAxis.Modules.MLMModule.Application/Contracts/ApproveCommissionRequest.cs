namespace CoreAxis.Modules.MLMModule.Application.Contracts;

public class ApproveCommissionRequest
{
    public Guid CommissionId { get; set; }
    public string? ApprovalNotes { get; set; }
}