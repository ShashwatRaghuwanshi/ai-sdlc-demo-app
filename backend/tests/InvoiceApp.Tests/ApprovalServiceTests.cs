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
    var invoice = InvoiceWith("INV-1", InvoiceStatus.Approved);
    var service = ServiceFor(invoice);

    var error = Assert.Throws<InvalidOperationException>(
        () => service.Submit("INV-1"));

    Assert.Equal(
        "Invoice INV-1 is Approved; only drafts can be submitted.",
        error.Message);
}


    [Fact]
    public void Submit_CallsRepositoryUpdateExactlyOnce()
    {
        var invoice = InvoiceWith("INV-UPDATE-SUBMIT", InvoiceStatus.Draft);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        service.Submit(invoice.Id);

        Assert.Equal(1, repository.UpdateCallCount);
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
    public void Approve_SuccessfulFinalApproval_CallsRepositoryUpdateExactlyOnce()
    {
        var invoice = InvoiceWith("INV-UPDATE-APPROVE", 50000m, InvoiceStatus.PendingApproval);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        service.Approve(invoice.Id, "manager-user", "Manager");

        Assert.Equal(1, repository.UpdateCallCount);
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
    public void Reject_SuccessfulReject_CallsRepositoryUpdateExactlyOnce()
    {
        var invoice = InvoiceWith("INV-UPDATE-REJECT", InvoiceStatus.PendingApproval);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        service.Reject(invoice.Id, "manager-user", "Manager", "Duplicate invoice");

        Assert.Equal(1, repository.UpdateCallCount);
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
    public void Director_ApprovalPersistsTheUpdate_WhenManagerStageAlreadyExists()
    {
        var managerStageAt = DateTime.UtcNow.AddDays(-2);
        var invoice = InvoiceWith("INV-500001-DIRECTOR-FINAL", 500001m, InvoiceStatus.PendingApproval);
        invoice.ManagerApproval = new InvoiceApprovalStage("manager-user", managerStageAt);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        service.Approve(invoice.Id, "director-user", "Director");

        Assert.Equal(1, repository.UpdateCallCount);
    }

    [Fact]
    public void Approve_InvalidApproval_DoesNotCallRepositoryUpdate()
    {
        var invoice = InvoiceWith("INV-UPDATE-INVALID-APPROVE", 50001m, InvoiceStatus.PendingApproval);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        Assert.Throws<InvalidOperationException>(() => service.Approve(invoice.Id, "manager-user", "Manager"));
        Assert.Equal(0, repository.UpdateCallCount);
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
    public void Reject_RequiresPendingInvoice()
    {
        var priorDecisionAt = DateTime.UtcNow.AddDays(-1);
        var invoice = InvoiceWith("INV-REJECT-APPROVED", 100m, InvoiceStatus.Approved, "prior-user", priorDecisionAt, "Already decided");
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        Assert.Throws<InvalidOperationException>(() => service.Reject(invoice.Id, "manager-user", "Manager", "Duplicate invoice"));
        Assert.Equal(InvoiceStatus.Approved, invoice.Status);
        Assert.Equal("prior-user", invoice.DecidedBy);
        Assert.Equal(priorDecisionAt, invoice.DecidedAt);
        Assert.Equal("Already decided", invoice.DecisionNote);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public void Manager_CannotFinallyApproveInvoiceAtExactly500000()
    {
        var invoice = InvoiceWith("INV-500000", 500000m, InvoiceStatus.PendingApproval);
        var service = ServiceFor(invoice);

        Assert.Throws<InvalidOperationException>(() => service.Approve(invoice.Id, "manager-user", "Manager"));
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
        Assert.Null(invoice.ManagerApproval);
        Assert.Null(invoice.DirectorApproval);
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
    public void Reject_ValidatesApproverRole()
    {
        var existingDecisionAt = DateTime.UtcNow.AddDays(-1);
        var invoice = InvoiceWith("INV-REJECT-INVALID-ROLE", 100m, InvoiceStatus.PendingApproval, "prior-user", existingDecisionAt, "Existing decision");
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        Assert.Throws<InvalidOperationException>(() => service.Reject(invoice.Id, "employee-user", "Employee", "Duplicate invoice"));
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal("prior-user", invoice.DecidedBy);
        Assert.Equal(existingDecisionAt, invoice.DecidedAt);
        Assert.Equal("Existing decision", invoice.DecisionNote);
        Assert.Equal(0, repository.UpdateCallCount);
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

    [Fact]
    public void Manager_FirstApproval_Above500000_RecordsStageAndLeavesInvoicePending()
    {
        var invoice = InvoiceWith("INV-500001-MANAGER-STAGE", 500001m, InvoiceStatus.PendingApproval);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-500001-MANAGER-STAGE", "manager-user", "Manager");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
        Assert.Null(result.DecisionNote);
        Assert.NotNull(result.ManagerApproval);
        Assert.Equal("manager-user", result.ManagerApproval!.ApproverUsername);
        Assert.NotEqual(default(DateTime), result.ManagerApproval.ApprovedAt);
    }

    [Fact]
    public void Director_SecondStageApproval_Above500000_WithDifferentDirector_CompletesApprovalAndRetainsManagerStage()
    {
        var managerStageAt = DateTime.UtcNow.AddDays(-2);
        var invoice = InvoiceWith("INV-500001-DIRECTOR-FINAL", 500001m, InvoiceStatus.PendingApproval);
        invoice.ManagerApproval = new InvoiceApprovalStage("manager-user", managerStageAt);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-500001-DIRECTOR-FINAL", "director-user", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("director-user", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.NotNull(result.ManagerApproval);
        Assert.Equal("manager-user", result.ManagerApproval!.ApproverUsername);
        Assert.Equal(managerStageAt, result.ManagerApproval.ApprovedAt);
        Assert.NotNull(result.DirectorApproval);
        Assert.Equal("director-user", result.DirectorApproval!.ApproverUsername);
    }

    [Fact]
    public void SameUser_CannotCompleteBothStagesForInvoiceAbove500000()
    {
        var managerStageAt = DateTime.UtcNow.AddDays(-2);
        var invoice = InvoiceWith("INV-500001-SAME-USER", 500001m, InvoiceStatus.PendingApproval);
        invoice.ManagerApproval = new InvoiceApprovalStage("manager-user", managerStageAt);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-500001-SAME-USER", "manager-user", "Director"));
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
        Assert.NotNull(invoice.ManagerApproval);
        Assert.Equal("manager-user", invoice.ManagerApproval!.ApproverUsername);
        Assert.Equal(managerStageAt, invoice.ManagerApproval.ApprovedAt);
        Assert.Null(invoice.DirectorApproval);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public void Director_CannotApproveInvoiceAbove500000_BeforeManagerStageExists()
    {
        var invoice = InvoiceWith("INV-500001-MISSING-MANAGER-STAGE", 500001m, InvoiceStatus.PendingApproval);
        var service = ServiceFor(invoice);

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-500001-MISSING-MANAGER-STAGE", "director-user", "Director"));
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
        Assert.Null(invoice.ManagerApproval);
        Assert.Null(invoice.DirectorApproval);
    }

    [Fact]
    public void InvalidApprovalAttempt_PreservesExistingFinalDecisionMetadataAndStageRecords()
    {
        var previousDecisionAt = DateTime.UtcNow.AddDays(-1);
        var invoice = InvoiceWith("INV-INVALID-STAGE-METADATA", 500001m, InvoiceStatus.PendingApproval, "existing.manager", previousDecisionAt, "Prior review");
        invoice.ManagerApproval = new InvoiceApprovalStage("existing.manager", previousDecisionAt.AddHours(-1));
        var service = ServiceFor(invoice);

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-INVALID-STAGE-METADATA", "employee-user", "Employee"));
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal("existing.manager", invoice.DecidedBy);
        Assert.Equal(previousDecisionAt, invoice.DecidedAt);
        Assert.Equal("Prior review", invoice.DecisionNote);
        Assert.NotNull(invoice.ManagerApproval);
        Assert.Equal("existing.manager", invoice.ManagerApproval!.ApproverUsername);
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
    public void Reject_AfterManagerStage_PreservesManagerApprovalAndSetsRejectedOutcome()
    {
        var managerStageAt = DateTime.UtcNow.AddDays(-2);
        var invoice = InvoiceWith("INV-500001-REJECTED-MANAGER-STAGE", 500001m, InvoiceStatus.PendingApproval);
        invoice.ManagerApproval = new InvoiceApprovalStage("manager-user", managerStageAt);
        var service = ServiceFor(invoice);

        var originalManagerApproval = invoice.ManagerApproval;

        var result = service.Reject("INV-500001-REJECTED-MANAGER-STAGE", "director-user", "Director", "Duplicate manager stage");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("director-user", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Equal("Duplicate manager stage", result.DecisionNote);
        Assert.NotNull(result.ManagerApproval);
        Assert.Equal(originalManagerApproval!.ApproverUsername, result.ManagerApproval!.ApproverUsername);
        Assert.Equal(originalManagerApproval.ApprovedAt, result.ManagerApproval.ApprovedAt);
        Assert.Null(result.DirectorApproval);

        Assert.Equal(InvoiceStatus.Rejected, invoice.Status);
        Assert.Equal("director-user", invoice.DecidedBy);
        Assert.NotNull(invoice.DecidedAt);
        Assert.Equal("Duplicate manager stage", invoice.DecisionNote);
        Assert.NotNull(invoice.ManagerApproval);
        Assert.Equal(originalManagerApproval.ApproverUsername, invoice.ManagerApproval!.ApproverUsername);
        Assert.Equal(originalManagerApproval.ApprovedAt, invoice.ManagerApproval.ApprovedAt);
        Assert.Null(invoice.DirectorApproval);
    }

    
[Fact]
public void Approve_WithBlankUsername_ThrowsWithoutMutatingInvoice()
{
    var invoice = InvoiceWith(
        "INV-500001-BLANK-USERNAME",
        500001m,
        InvoiceStatus.PendingApproval);
    var service = ServiceFor(invoice);

    var originalStatus = invoice.Status;
    var originalDecisionBy = invoice.DecidedBy;
    var originalDecisionAt = invoice.DecidedAt;
    var originalDecisionNote = invoice.DecisionNote;
    var originalManagerApproval = invoice.ManagerApproval;
    var originalDirectorApproval = invoice.DirectorApproval;

    var error = Assert.Throws<InvalidOperationException>(
        () => service.Approve(
            invoice.Id, "   ", "Manager"));

    Assert.Equal(
        "Approver username is required for final approval.",
        error.Message);

    Assert.Equal(originalStatus, invoice.Status);
    Assert.Equal(originalDecisionBy, invoice.DecidedBy);
    Assert.Equal(originalDecisionAt, invoice.DecidedAt);
    Assert.Equal(originalDecisionNote, invoice.DecisionNote);
    Assert.Equal(originalManagerApproval, invoice.ManagerApproval);
    Assert.Equal(originalDirectorApproval, invoice.DirectorApproval);
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

    



    [Fact]
    public void Manager_CannotFinallyApproveInvoiceOf50000Point01()
    {
        var invoice = InvoiceWith(
            "INV-50000-01",
            50000.01m,
            InvoiceStatus.PendingApproval);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "manager-user", "Manager"));

        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.ManagerApproval);
        Assert.Null(invoice.DirectorApproval);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public void Director_CannotFinallyApproveInvoiceOf500000Point01WithoutManagerStage()
    {
        var invoice = InvoiceWith(
            "INV-500000-01",
            500000.01m,
            InvoiceStatus.PendingApproval);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "director-user", "Director"));

        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.ManagerApproval);
        Assert.Null(invoice.DirectorApproval);
        Assert.Equal(0, repository.UpdateCallCount);
    }
}

