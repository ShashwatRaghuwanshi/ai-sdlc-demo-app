using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(string id, InvoiceStatus status) =>
        new() { Id = id, CustomerName = "Test School", Amount = 100m, Status = status };

    private static Invoice InvoiceWith(string id, decimal amount, InvoiceStatus status, string? decidedBy = null, DateTime? decidedAt = null, string? decisionNote = null) =>
        new() { Id = id, CustomerName = "Test School", Amount = amount, Status = status, DecidedBy = decidedBy, DecidedAt = decidedAt, DecisionNote = decisionNote };

    private static ApprovalService ServiceFor(params Invoice[] invoices) =>
        new(new FakeInvoiceRepository(invoices));

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);

        var result = ServiceFor(invoice).Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));
    }

    [Fact]
    public void Approve_SetsStatusApproverAndTimestamp()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Approve("INV-1", "demo.approver", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.approver", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "demo.approver", "Manager"));
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var service = ServiceFor();

        Assert.Throws<KeyNotFoundException>(() => service.Approve("INV-404", "demo.approver", "Manager"));
    }

    [Fact]
    public void Reject_RecordsReasonAndApprover()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Reject("INV-1", "demo.approver", "Manager", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        Assert.Throws<ArgumentException>(() => service.Reject("INV-1", "demo.approver", "Manager", "  "));
    }

    [Fact]
    public void Manager_CanFinallyApproveInvoiceAtExactly50000()
    {
        var invoice = InvoiceWith("INV-50000", 50000m, InvoiceStatus.PendingApproval);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-50000", "manager-user", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("manager-user", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Manager_CannotFinallyApproveInvoiceAbove50000()
    {
        var invoice = InvoiceWith("INV-50001", 50001m, InvoiceStatus.PendingApproval);
        var service = ServiceFor(invoice);

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-50001", "manager-user", "Manager"));
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
    }

    [Fact]
    public void Director_CanFinallyApproveInvoiceAtExactly500000()
    {
        var invoice = InvoiceWith("INV-500000", 500000m, InvoiceStatus.PendingApproval);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-500000", "director-user", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("director-user", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Director_CannotFinallyApproveInvoiceAbove500000WithoutManagerStage()
    {
        var invoice = InvoiceWith("INV-500001", 500001m, InvoiceStatus.PendingApproval);
        var service = ServiceFor(invoice);

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-500001", "director-user", "Director"));
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
    }

    [Fact]
    public void Director_CannotFinallyApproveInvoiceOfExactly50000()
    {
        var invoice = InvoiceWith("INV-50000-DIRECTOR", 50000m, InvoiceStatus.PendingApproval);
        var service = ServiceFor(invoice);

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-50000-DIRECTOR", "director-user", "Director"));
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
    }

    [Fact]
    public void Director_CanFinallyApproveInvoiceOf50001()
    {
        var invoice = InvoiceWith("INV-50001-DIRECTOR", 50001m, InvoiceStatus.PendingApproval);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-50001-DIRECTOR", "director-user", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("director-user", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Manager_CanFinallyApproveInvoiceOf1()
    {
        var invoice = InvoiceWith("INV-1-MANAGER", 1m, InvoiceStatus.PendingApproval);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1-MANAGER", "manager-user", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("manager-user", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void MissingOrWhitespaceApproverRole_IsRejectedWithoutChangingInvoiceState(string? approverRole)
    {
        var previousDecisionAt = DateTime.UtcNow.AddDays(-1);
        var invoice = InvoiceWith("INV-EMPTY-ROLE", 25000m, InvoiceStatus.PendingApproval, "existing.manager", previousDecisionAt, "Previous review");
        var service = ServiceFor(invoice);

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-EMPTY-ROLE", "manager-user", approverRole!));
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal("existing.manager", invoice.DecidedBy);
        Assert.Equal(previousDecisionAt, invoice.DecidedAt);
        Assert.Equal("Previous review", invoice.DecisionNote);
    }

    [Fact]
    public void InvalidRole_CannotPerformFinalApproval()
    {
        var invoice = InvoiceWith("INV-INVALID-ROLE", 25000m, InvoiceStatus.PendingApproval);
        var service = ServiceFor(invoice);

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-INVALID-ROLE", "employee-user", "Employee"));
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
    }

    [Fact]
    public void RejectedApprovalAttempts_PreserveStatusAndExistingDecisionMetadata()
    {
        var previousDecisionAt = DateTime.UtcNow.AddDays(-1);
        var invoice = InvoiceWith("INV-REJECTED", 25000m, InvoiceStatus.PendingApproval, "existing.manager", previousDecisionAt, "Prior review");
        var service = ServiceFor(invoice);

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-REJECTED", "employee-user", "Employee"));
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal("existing.manager", invoice.DecidedBy);
        Assert.Equal(previousDecisionAt, invoice.DecidedAt);
        Assert.Equal("Prior review", invoice.DecisionNote);
    }
}
