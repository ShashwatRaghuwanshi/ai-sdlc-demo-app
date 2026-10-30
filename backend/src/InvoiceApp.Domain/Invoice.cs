namespace InvoiceApp.Domain;

public sealed record InvoiceApprovalStage(string ApproverUsername, DateTime ApprovedAt);

public class Invoice
{
    public string Id { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNote { get; set; }
    public InvoiceApprovalStage? ManagerApproval { get; set; }
    public InvoiceApprovalStage? DirectorApproval { get; set; }
}
