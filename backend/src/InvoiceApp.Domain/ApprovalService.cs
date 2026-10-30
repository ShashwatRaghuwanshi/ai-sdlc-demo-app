namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// Invoices above 500,000 require a Manager first-stage approval and a different Director final approval.
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
        if (string.IsNullOrWhiteSpace(approverUsername))
        {
            throw new InvalidOperationException("Approver username is required for final approval.");
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);
        EnsurePositiveAmount(invoice);

        var normalizedRole = NormalizeRole(approverRole);
        var normalizedUsername = approverUsername.Trim();

        if (normalizedRole.Equals("Manager", StringComparison.OrdinalIgnoreCase))
        {
            ApplyManagerApproval(invoice, normalizedUsername);
            _repository.Update(invoice);
            return invoice;
        }

        if (normalizedRole.Equals("Director", StringComparison.OrdinalIgnoreCase))
        {
            ApplyDirectorApproval(invoice, normalizedUsername);
            _repository.Update(invoice);
            return invoice;
        }

        throw new InvalidOperationException(
            $"Role '{approverRole}' is not allowed to provide final approval.");
    }

    public Invoice Reject(string invoiceId, string approverUsername, string approverRole, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
        }

        if (string.IsNullOrWhiteSpace(approverUsername))
        {
            throw new InvalidOperationException("Approver username is required to reject an invoice.");
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);
        ValidateRejectRole(approverRole);

        invoice.Status = InvoiceStatus.Rejected;
        invoice.DecidedBy = approverUsername.Trim();
        invoice.DecidedAt = DateTime.UtcNow;
        invoice.DecisionNote = reason;
        _repository.Update(invoice);
        return invoice;
    }

    private Invoice Find(string invoiceId) =>
        _repository.GetById(invoiceId)
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");


    private static void EnsurePositiveAmount(Invoice invoice)
    {
        if (invoice.Amount <= 0m)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} must have an amount greater than zero.");
        }
    }
    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }

    private static string NormalizeRole(string approverRole)
    {
        if (string.IsNullOrWhiteSpace(approverRole))
        {
            throw new InvalidOperationException("Approver role is required for final approval.");
        }

        return approverRole.Trim();
    }

    private static void ApplyManagerApproval(Invoice invoice, string approverUsername)
    {
        if (invoice.Amount <= 50000m)
        {
            ApplyFinalApproval(invoice, approverUsername);
            return;
        }

        if (invoice.Amount <= 500000m)
        {
            throw new InvalidOperationException(
                $"Manager final approval is only allowed for invoices up to 50,000. Invoice {invoice.Id} is {invoice.Amount:C}.");
        }

        if (invoice.ManagerApproval is not null)
        {
            throw new InvalidOperationException(
                $"Manager approval has already been recorded for invoice {invoice.Id}.");
        }

        invoice.ManagerApproval = new InvoiceApprovalStage(approverUsername, DateTime.UtcNow);
    }

    private static void ApplyDirectorApproval(Invoice invoice, string approverUsername)
    {
        if (invoice.Amount <= 50000m)
        {
            throw new InvalidOperationException(
                $"Director final approval is only allowed for invoices above 50,000 and up to 500,000. Invoice {invoice.Id} is {invoice.Amount:C}.");
        }

        if (invoice.Amount <= 500000m)
        {
            ApplyFinalApproval(invoice, approverUsername);
            return;
        }

        if (invoice.ManagerApproval is null)
        {
            throw new InvalidOperationException(
                $"Director final approval requires a prior Manager approval for invoices above 500,000. Invoice {invoice.Id} is {invoice.Amount:C}.");
        }

        if (string.Equals(invoice.ManagerApproval.ApproverUsername, approverUsername, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The same user cannot approve both stages for invoice {invoice.Id}.");
        }

        if (invoice.DirectorApproval is not null)
        {
            throw new InvalidOperationException(
                $"Director approval has already been recorded for invoice {invoice.Id}.");
        }

        var approvedAt = DateTime.UtcNow;
        invoice.DirectorApproval = new InvoiceApprovalStage(approverUsername, approvedAt);
        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approverUsername;
        invoice.DecidedAt = approvedAt;
    }

    private static void ApplyFinalApproval(Invoice invoice, string approverUsername)
    {
        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approverUsername;
        invoice.DecidedAt = DateTime.UtcNow;
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


