using Xunit;
using DbDemo.ConsoleApp.Models;
using FluentAssertions;

namespace DbDemo.Domain.Tests;

public class LoanTests
{
    [Fact]
    public void Create_CreatesLoanWithCorrectDefaults()
    {
        // Act
        var loan = Loan.Create(memberId: 1, bookId: 2);

        // Assert
        loan.MemberId.Should().Be(1);
        loan.BookId.Should().Be(2);
        loan.BorrowedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        loan.DueDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(14), TimeSpan.FromSeconds(1));
        loan.ReturnedAt.Should().BeNull();
        loan.Status.Should().Be(LoanStatus.Active);
        loan.RenewalCount.Should().Be(0);
        loan.MaxRenewalsAllowed.Should().Be(2);
        loan.IsFeePaid.Should().BeFalse();
        loan.LateFee.Should().BeNull();
    }

    [Fact]
    public void IsOverdue_WhenNotReturnedAndPastDueDate_ReturnsTrue()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        // Simulate loan is overdue by setting DueDate to past
        var dueDateProperty = typeof(Loan).GetProperty("DueDate");
        dueDateProperty!.SetValue(loan, DateTime.UtcNow.AddDays(-1));

        // Act & Assert
        loan.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WhenNotReturnedAndNotPastDueDate_ReturnsFalse()
    {
        // Arrange
        var loan = Loan.Create(1, 2);

        // Act & Assert
        loan.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenReturned_ReturnsFalse()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        loan.Return();

        // Act & Assert
        loan.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void DaysOverdue_WhenNotOverdue_ReturnsZero()
    {
        // Arrange
        var loan = Loan.Create(1, 2);

        // Act & Assert
        loan.DaysOverdue.Should().Be(0);
    }

    [Fact]
    public void DaysOverdue_WhenOverdue_ReturnsCorrectDays()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        var dueDateProperty = typeof(Loan).GetProperty("DueDate");
        dueDateProperty!.SetValue(loan, DateTime.UtcNow.AddDays(-3));

        // Act & Assert
        loan.DaysOverdue.Should().Be(3);
    }

    [Fact]
    public void DaysOverdue_WhenReturned_ReturnsZero()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        var dueDateProperty = typeof(Loan).GetProperty("DueDate");
        dueDateProperty!.SetValue(loan, DateTime.UtcNow.AddDays(-3));
        loan.Return();

        // Act & Assert
        loan.DaysOverdue.Should().Be(0);
    }

    [Fact]
    public void CanBeRenewed_WhenEligible_ReturnsTrue()
    {
        // Arrange
        var loan = Loan.Create(1, 2);

        // Act & Assert
        loan.CanBeRenewed.Should().BeTrue();
    }

    [Fact]
    public void CanBeRenewed_WhenOverdue_ReturnsFalse()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        var dueDateProperty = typeof(Loan).GetProperty("DueDate");
        dueDateProperty!.SetValue(loan, DateTime.UtcNow.AddDays(-1));

        // Act & Assert
        loan.CanBeRenewed.Should().BeFalse();
    }

    [Fact]
    public void CanBeRenewed_WhenMaxRenewalsReached_ReturnsFalse()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        loan.Renew();
        loan.Renew();

        // Act & Assert
        loan.CanBeRenewed.Should().BeFalse();
        loan.RenewalCount.Should().Be(2);
    }

    [Fact]
    public void CanBeRenewed_WhenStatusNotActive_ReturnsFalse()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        loan.Return();

        // Act & Assert
        loan.CanBeRenewed.Should().BeFalse();
    }

    [Fact]
    public void Renew_WhenEligible_ExtendsDueDateAndIncrementsRenewalCount()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        var originalDueDate = loan.DueDate;

        // Act
        loan.Renew();

        // Assert
        loan.DueDate.Should().Be(originalDueDate.AddDays(14));
        loan.RenewalCount.Should().Be(1);
    }

    [Fact]
    public void Renew_WithCustomDays_ExtendsDueDateBySpecifiedDays()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        var originalDueDate = loan.DueDate;

        // Act
        loan.Renew(additionalDays: 7);

        // Assert
        loan.DueDate.Should().Be(originalDueDate.AddDays(7));
    }

    [Fact]
    public void Renew_WhenNotEligible_ThrowsInvalidOperationException()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        var dueDateProperty = typeof(Loan).GetProperty("DueDate");
        dueDateProperty!.SetValue(loan, DateTime.UtcNow.AddDays(-1)); // Make overdue

        // Act
        Action act = () => loan.Renew();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Loan cannot be renewed*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Renew_WithInvalidDays_ThrowsArgumentException(int invalidDays)
    {
        // Arrange
        var loan = Loan.Create(1, 2);

        // Act
        Action act = () => loan.Renew(invalidDays);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Additional days must be positive*");
    }

    [Fact]
    public void Return_WhenNotOverdue_SetsStatusToReturned()
    {
        // Arrange
        var loan = Loan.Create(1, 2);

        // Act
        loan.Return();

        // Assert
        loan.ReturnedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        loan.Status.Should().Be(LoanStatus.Returned);
        loan.LateFee.Should().Be(0);
    }

    [Fact]
    public void Return_WhenOverdue_SetsStatusToReturnedLateAndCalculatesFee()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        var dueDateProperty = typeof(Loan).GetProperty("DueDate");
        dueDateProperty!.SetValue(loan, DateTime.UtcNow.AddDays(-3));

        // Act
        loan.Return();

        // Assert
        loan.ReturnedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        loan.Status.Should().Be(LoanStatus.ReturnedLate);
        loan.LateFee.Should().BeGreaterThan(0);
        loan.LateFee.Should().Be(1.50m); // 3 days * $0.50/day
    }

    [Fact]
    public void Return_WhenAlreadyReturned_ThrowsInvalidOperationException()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        loan.Return();

        // Act
        Action act = () => loan.Return();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Loan has already been returned*");
    }

    [Fact]
    public void CalculateLateFee_WhenNotOverdue_ReturnsZero()
    {
        // Arrange
        var loan = Loan.Create(1, 2);

        // Act
        var fee = loan.CalculateLateFee();

        // Assert
        fee.Should().Be(0);
    }

    [Fact]
    public void CalculateLateFee_WhenOverdue_CalculatesCorrectFee()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        var dueDateProperty = typeof(Loan).GetProperty("DueDate");
        dueDateProperty!.SetValue(loan, DateTime.UtcNow.AddDays(-5));

        // Act
        var fee = loan.CalculateLateFee();

        // Assert
        fee.Should().Be(2.50m); // 5 days * $0.50/day
    }

    [Fact]
    public void CalculateLateFee_AfterReturn_UsesReturnDate()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        var dueDateProperty = typeof(Loan).GetProperty("DueDate");
        dueDateProperty!.SetValue(loan, DateTime.UtcNow.AddDays(-3));
        loan.Return();

        // Act
        var fee = loan.CalculateLateFee();

        // Assert
        fee.Should().Be(1.50m); // 3 days * $0.50/day (calculated at return time)
    }

    [Fact]
    public void MarkAsLost_WhenNotReturned_ChangesStatusToLost()
    {
        // Arrange
        var loan = Loan.Create(1, 2);

        // Act
        loan.MarkAsLost();

        // Assert
        loan.Status.Should().Be(LoanStatus.Lost);
    }

    [Fact]
    public void MarkAsLost_WhenAlreadyReturned_ThrowsInvalidOperationException()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        loan.Return();

        // Act
        Action act = () => loan.MarkAsLost();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot mark returned loan as lost*");
    }

    [Fact]
    public void MarkAsDamaged_WhenReturned_ChangesStatusAndSetsNotes()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        loan.Return();

        // Act
        loan.MarkAsDamaged("Water damage on cover");

        // Assert
        loan.Status.Should().Be(LoanStatus.Damaged);
        loan.Notes.Should().Be("Water damage on cover");
    }

    [Fact]
    public void MarkAsDamaged_WhenNotReturned_ThrowsInvalidOperationException()
    {
        // Arrange
        var loan = Loan.Create(1, 2);

        // Act
        Action act = () => loan.MarkAsDamaged("Some damage");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Book must be returned before marking as damaged*");
    }

    [Fact]
    public void PayLateFee_WhenFeeExists_MarksFeeAsPaid()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        var dueDateProperty = typeof(Loan).GetProperty("DueDate");
        dueDateProperty!.SetValue(loan, DateTime.UtcNow.AddDays(-3));
        loan.Return();

        // Act
        loan.PayLateFee();

        // Assert
        loan.IsFeePaid.Should().BeTrue();
    }

    [Fact]
    public void PayLateFee_WhenNoFee_ThrowsInvalidOperationException()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        loan.Return(); // Return on time, no fee

        // Act
        Action act = () => loan.PayLateFee();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No late fee to pay*");
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var loan = Loan.Create(memberId: 1, bookId: 2);

        // Act
        var result = loan.ToString();

        // Assert
        result.Should().Contain("Book 2");
        result.Should().Contain("Member 1");
        result.Should().Contain("Active");
    }

    [Fact]
    public void ToString_WhenOverdue_ShowsOverdueStatus()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        var dueDateProperty = typeof(Loan).GetProperty("DueDate");
        dueDateProperty!.SetValue(loan, DateTime.UtcNow.AddDays(-1));

        // Act
        var result = loan.ToString();

        // Assert
        result.Should().Contain("Overdue");
    }

    [Fact]
    public void ToString_WhenReturned_ShowsReturnedStatus()
    {
        // Arrange
        var loan = Loan.Create(1, 2);
        loan.Return();

        // Act
        var result = loan.ToString();

        // Assert
        result.Should().Contain("Returned");
    }
}
