namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// One invoice at a time, decided as a whole by a single approver.
/// </summary>
public class ApprovalService
{
    private readonly IInvoiceRepository _repository;

    public ApprovalService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Invoice Submit(string invoiceId)
    {
        var invoice = Find(invoiceId);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted.");
        }

        invoice.Status = InvoiceStatus.PendingApproval;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Approve(string invoiceId, string approverUsername, string approverRole)
    {
        var invoice = Find(invoiceId);
        EnsurePending(invoice);
        ValidateFinalApproval(invoice, approverRole);

        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approverUsername;
        invoice.DecidedAt = DateTime.UtcNow;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Reject(string invoiceId, string approverUsername, string approverRole, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);
        ValidateRejectRole(approverRole);

        invoice.Status = InvoiceStatus.Rejected;
        invoice.DecidedBy = approverUsername;
        invoice.DecidedAt = DateTime.UtcNow;
        invoice.DecisionNote = reason;
        _repository.Update(invoice);
        return invoice;
    }

    private Invoice Find(string invoiceId) =>
        _repository.GetById(invoiceId)
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }

    private static void ValidateFinalApproval(Invoice invoice, string approverRole)
    {
        if (string.IsNullOrWhiteSpace(approverRole))
        {
            throw new InvalidOperationException("Approver role is required for final approval.");
        }

        var normalizedRole = approverRole.Trim();

        if (normalizedRole.Equals("Manager", StringComparison.OrdinalIgnoreCase))
        {
            if (invoice.Amount > 50000m)
            {
                throw new InvalidOperationException(
                    $"Manager final approval is only allowed for invoices up to 50,000. Invoice {invoice.Id} is {invoice.Amount:C}.");
            }

            return;
        }

        if (normalizedRole.Equals("Director", StringComparison.OrdinalIgnoreCase))
        {
            if (invoice.Amount <= 50000m || invoice.Amount > 500000m)
            {
                throw new InvalidOperationException(
                    $"Director final approval is only allowed for invoices above 50,000 and up to 500,000. Invoice {invoice.Id} is {invoice.Amount:C}.");
            }

            return;
        }

        throw new InvalidOperationException(
            $"Role '{approverRole}' is not allowed to provide final approval.");
    }

    private static void ValidateRejectRole(string approverRole)
    {
        if (string.IsNullOrWhiteSpace(approverRole))
        {
            throw new InvalidOperationException("Approver role is required to reject an invoice.");
        }

        var normalizedRole = approverRole.Trim();

        if (normalizedRole.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
            normalizedRole.Equals("Director", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Role '{approverRole}' is not allowed to reject an invoice.");
    }
}
