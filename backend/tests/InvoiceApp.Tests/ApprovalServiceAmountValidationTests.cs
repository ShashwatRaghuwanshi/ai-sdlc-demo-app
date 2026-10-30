using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceAmountValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Approve_InvalidAmount_ThrowsWithoutMutatingInvoice(decimal amount)
    {
        var invoice = new Invoice
        {
            Id = "INV-INVALID-AMOUNT",
            CustomerName = "Test School",
            Amount = amount,
            Status = InvoiceStatus.PendingApproval
        };
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "manager-user", "Manager"));

        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
        Assert.Null(invoice.ManagerApproval);
        Assert.Null(invoice.DirectorApproval);
        Assert.Equal(0, repository.UpdateCallCount);
    }
}
