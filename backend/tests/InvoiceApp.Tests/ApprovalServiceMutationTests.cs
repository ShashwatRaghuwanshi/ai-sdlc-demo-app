using System.Collections.Generic;
using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceMutationTests
{
    private static Invoice CreateInvoice(
        string id,
        decimal amount = 100m,
        InvoiceStatus status = InvoiceStatus.PendingApproval)
    {
        return new Invoice
        {
            Id = id,
            CustomerName = "Test School",
            Amount = amount,
            Status = status
        };
    }

    [Fact]
    public void Approve_InvalidRole_ReturnsMeaningfulError()
    {
        var invoice = CreateInvoice("INV-INVALID-ROLE");
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var error = Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "employee", "Employee"));

        Assert.Equal(
            "Role 'Employee' is not allowed to provide final approval.",
            error.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public void Reject_BlankReason_ReturnsMeaningfulError()
    {
        var invoice = CreateInvoice("INV-BLANK-REASON");
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var error = Assert.Throws<ArgumentException>(
            () => service.Reject(invoice.Id, "manager", "Manager", " "));

        Assert.Contains(
            "A reason is required to reject an invoice.",
            error.Message);
        Assert.Equal("reason", error.ParamName);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public void Reject_BlankUsername_ReturnsMeaningfulError()
    {
        var invoice = CreateInvoice("INV-BLANK-REJECT-USER");
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var error = Assert.Throws<InvalidOperationException>(
            () => service.Reject(invoice.Id, " ", "Manager", "Duplicate"));

        Assert.Equal(
            "Approver username is required to reject an invoice.",
            error.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public void Approve_MissingInvoice_IdentifiesInvoice()
    {
        var service = new ApprovalService(new FakeInvoiceRepository());

        var error = Assert.Throws<KeyNotFoundException>(
            () => service.Approve("INV-MISSING", "manager", "Manager"));

        Assert.Equal(
            "Invoice INV-MISSING was not found.",
            error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Approve_NonPositiveAmount_ReturnsMeaningfulError(
        int amount)
    {
        var invoice = CreateInvoice("INV-NONPOSITIVE", amount);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var error = Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "manager", "Manager"));

        Assert.Equal(
            "Invoice INV-NONPOSITIVE must have an amount greater than zero.",
            error.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public void Approve_NonPendingInvoice_ReturnsMeaningfulError()
    {
        var invoice = CreateInvoice(
            "INV-NOT-PENDING", 100m, InvoiceStatus.Draft);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var error = Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "manager", "Manager"));

        Assert.Equal(
            "Invoice INV-NOT-PENDING is Draft; only pending invoices can be decided.",
            error.Message);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public void Approve_BlankRole_ReturnsMeaningfulError()
    {
        var invoice = CreateInvoice("INV-BLANK-ROLE");
        var service = new ApprovalService(
            new FakeInvoiceRepository(invoice));

        var error = Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "manager", " "));

        Assert.Equal(
            "Approver role is required for final approval.",
            error.Message);
    }

    [Fact]
    public void Manager_CannotFinallyApproveMidRangeInvoice()
    {
        var invoice = CreateInvoice("INV-MANAGER-LIMIT", 50001m);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var error = Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "manager", "Manager"));

        Assert.Contains(
            "Manager final approval is only allowed for invoices up to 50,000.",
            error.Message);
        Assert.Contains(invoice.Id, error.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public void Manager_CannotOverwriteExistingFirstStage()
    {
        var invoice = CreateInvoice("INV-DUPLICATE-MANAGER", 500001m);
        var originalStage = new InvoiceApprovalStage(
            "first-manager", DateTime.UtcNow.AddDays(-1));
        invoice.ManagerApproval = originalStage;

        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var error = Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "second-manager", "Manager"));

        Assert.Equal(
            "Manager approval has already been recorded for invoice INV-DUPLICATE-MANAGER.",
            error.Message);
        Assert.Same(originalStage, invoice.ManagerApproval);
        Assert.Null(invoice.DirectorApproval);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public void Director_CannotFinallyApproveLowValueInvoice()
    {
        var invoice = CreateInvoice("INV-DIRECTOR-LOW", 50000m);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var error = Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "director", "Director"));

        Assert.Contains(
            "Director final approval is only allowed for invoices above 50,000 and up to 500,000.",
            error.Message);
        Assert.Contains(invoice.Id, error.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public void Director_RequiresManagerStageForHighValueInvoice()
    {
        var invoice = CreateInvoice("INV-MISSING-STAGE", 500001m);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var error = Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "director", "Director"));

        Assert.Contains(
            "Director final approval requires a prior Manager approval",
            error.Message);
        Assert.Contains(invoice.Id, error.Message);
        Assert.Null(invoice.DirectorApproval);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public void Director_CannotBeSameUserAsManager()
    {
        var invoice = CreateInvoice("INV-SAME-USER", 500001m);
        var managerStage = new InvoiceApprovalStage(
            "manager-user", DateTime.UtcNow.AddDays(-1));
        invoice.ManagerApproval = managerStage;

        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var error = Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "MANAGER-USER", "Director"));

        Assert.Equal(
            "The same user cannot approve both stages for invoice INV-SAME-USER.",
            error.Message);
        Assert.Same(managerStage, invoice.ManagerApproval);
        Assert.Null(invoice.DirectorApproval);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public void Reject_BlankRole_ReturnsMeaningfulError()
    {
        var invoice = CreateInvoice("INV-REJECT-BLANK-ROLE");
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var error = Assert.Throws<InvalidOperationException>(
            () => service.Reject(invoice.Id, "manager", " ", "Duplicate"));

        Assert.Equal(
            "Approver role is required to reject an invoice.",
            error.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public void Reject_InvalidRole_ReturnsMeaningfulError()
    {
        var invoice = CreateInvoice("INV-REJECT-INVALID-ROLE");
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var error = Assert.Throws<InvalidOperationException>(
            () => service.Reject(
                invoice.Id, "employee", "Employee", "Duplicate"));

        Assert.Equal(
            "Role 'Employee' is not allowed to reject an invoice.",
            error.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
public void Director_CannotOverwriteExistingDirectorApproval()
{
    var invoice = CreateInvoice("INV-DUPLICATE-DIRECTOR", 500001m);

    var managerStage = new InvoiceApprovalStage(
        "manager-user", DateTime.UtcNow.AddDays(-2));

    var directorStage = new InvoiceApprovalStage(
        "first-director", DateTime.UtcNow.AddDays(-1));

    invoice.ManagerApproval = managerStage;
    invoice.DirectorApproval = directorStage;

    var repository = new FakeInvoiceRepository(invoice);
    var service = new ApprovalService(repository);

    var error = Assert.Throws<InvalidOperationException>(
        () => service.Approve(
            invoice.Id, "second-director", "Director"));

    Assert.Equal(
        "Director approval has already been recorded for invoice INV-DUPLICATE-DIRECTOR.",
        error.Message);

    Assert.Same(managerStage, invoice.ManagerApproval);
    Assert.Same(directorStage, invoice.DirectorApproval);
    Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
    Assert.Null(invoice.DecidedBy);
    Assert.Null(invoice.DecidedAt);
    Assert.Equal(0, repository.UpdateCallCount);
}

[Fact]
    public void Invoice_DefaultId_IsEmpty()
    {
        var invoice = new Invoice();

        Assert.Equal(string.Empty, invoice.Id);
    }

    
[Fact]
public void Invoice_DefaultCustomerName_IsEmpty()
{
    var invoice = new Invoice();

    Assert.Equal(string.Empty, invoice.CustomerName);
}

}
