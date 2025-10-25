using Xunit;
using DbDemo.ConsoleApp.Models;
using FluentAssertions;

namespace DbDemo.Domain.Tests;

public class MemberTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesMember()
    {
        // Arrange
        var dateOfBirth = new DateTime(1990, 5, 15);

        // Act
        var member = new Member("LIB-2024-001", "John", "Doe", "john.doe@example.com", dateOfBirth);

        // Assert
        member.MembershipNumber.Should().Be("LIB-2024-001");
        member.FirstName.Should().Be("John");
        member.LastName.Should().Be("Doe");
        member.FullName.Should().Be("John Doe");
        member.Email.Should().Be("john.doe@example.com");
        member.DateOfBirth.Should().Be(dateOfBirth);
        member.IsActive.Should().BeTrue();
        member.MaxBooksAllowed.Should().Be(5);
        member.OutstandingFees.Should().Be(0);
        member.MemberSince.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        member.MembershipExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddYears(1), TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null, "Doe")]
    [InlineData("", "Doe")]
    [InlineData("   ", "Doe")]
    [InlineData("John", null)]
    [InlineData("John", "")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? firstName, string? lastName)
    {
        // Act
        Action act = () => new Member("LIB-001", firstName!, lastName!, "john@example.com", DateTime.Today);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void Constructor_WithInvalidEmail_ThrowsArgumentException(string invalidEmail)
    {
        // Act
        Action act = () => new Member("LIB-001", "John", "Doe", invalidEmail, DateTime.Today);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email format*");
    }

    [Fact]
    public void Constructor_NormalizesEmailToLowerCase()
    {
        // Act
        var member = new Member("LIB-001", "John", "Doe", "John.Doe@EXAMPLE.COM", DateTime.Today);

        // Assert
        member.Email.Should().Be("john.doe@example.com");
    }

    [Fact]
    public void FullName_CombinesFirstAndLastName()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Q. Doe", "john@example.com", DateTime.Today);

        // Act & Assert
        member.FullName.Should().Be("John Q. Doe");
    }

    [Fact]
    public void Age_CalculatesCorrectAge()
    {
        // Arrange
        var dateOfBirth = DateTime.Today.AddYears(-25);
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", dateOfBirth);

        // Act & Assert
        member.Age.Should().Be(25);
    }

    [Fact]
    public void Age_BeforeBirthday_CalculatesCorrectAge()
    {
        // Arrange
        var dateOfBirth = DateTime.Today.AddYears(-25).AddDays(1); // Birthday tomorrow
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", dateOfBirth);

        // Act & Assert
        member.Age.Should().Be(24);
    }

    [Fact]
    public void IsMembershipValid_WhenActiveAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);

        // Act & Assert
        member.IsMembershipValid.Should().BeTrue();
    }

    [Fact]
    public void IsMembershipValid_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);
        // Manually set expiration to past (in real scenario, this would be done by time passing)
        var expirationProperty = typeof(Member).GetProperty("MembershipExpiresAt");
        expirationProperty!.SetValue(member, DateTime.UtcNow.AddDays(-1));

        // Act & Assert
        member.IsMembershipValid.Should().BeFalse();
    }

    [Fact]
    public void IsMembershipValid_WhenInactive_ReturnsFalse()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);
        member.Deactivate();

        // Act & Assert
        member.IsMembershipValid.Should().BeFalse();
    }

    [Fact]
    public void ExtendMembership_ExtendsExpirationDate()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);
        var originalExpiration = member.MembershipExpiresAt;

        // Act
        member.ExtendMembership(6);

        // Assert
        member.MembershipExpiresAt.Should().Be(originalExpiration.AddMonths(6));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ExtendMembership_WithInvalidMonths_ThrowsArgumentException(int invalidMonths)
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);

        // Act
        Action act = () => member.ExtendMembership(invalidMonths);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Months must be positive*");
    }

    [Fact]
    public void Activate_SetsMemberAsActive()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);
        member.Deactivate();

        // Act
        member.Activate();

        // Assert
        member.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_SetsMemberAsInactive()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);

        // Act
        member.Deactivate();

        // Assert
        member.IsActive.Should().BeFalse();
    }

    [Fact]
    public void AddFee_IncreasesOutstandingFees()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);

        // Act
        member.AddFee(5.50m);

        // Assert
        member.OutstandingFees.Should().Be(5.50m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddFee_WithInvalidAmount_ThrowsArgumentException(decimal invalidAmount)
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);

        // Act
        Action act = () => member.AddFee(invalidAmount);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Fee amount must be positive*");
    }

    [Fact]
    public void PayFee_DecreasesOutstandingFees()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);
        member.AddFee(10.00m);

        // Act
        member.PayFee(6.00m);

        // Assert
        member.OutstandingFees.Should().Be(4.00m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PayFee_WithInvalidAmount_ThrowsArgumentException(decimal invalidAmount)
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);
        member.AddFee(10.00m);

        // Act
        Action act = () => member.PayFee(invalidAmount);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Payment amount must be positive*");
    }

    [Fact]
    public void PayFee_MoreThanOutstanding_ThrowsArgumentException()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);
        member.AddFee(5.00m);

        // Act
        Action act = () => member.PayFee(10.00m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Payment amount exceeds outstanding fees*");
    }

    [Fact]
    public void CanBorrowBooks_WhenEligible_ReturnsTrue()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);

        // Act & Assert
        member.CanBorrowBooks().Should().BeTrue();
    }

    [Fact]
    public void CanBorrowBooks_WhenMembershipInvalid_ReturnsFalse()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);
        member.Deactivate();

        // Act & Assert
        member.CanBorrowBooks().Should().BeFalse();
    }

    [Fact]
    public void CanBorrowBooks_WhenFeesExceedThreshold_ReturnsFalse()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);
        member.AddFee(15.00m); // Over $10 threshold

        // Act & Assert
        member.CanBorrowBooks().Should().BeFalse();
    }

    [Fact]
    public void CanBorrowBooks_WhenFeesAtThreshold_ReturnsTrue()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);
        member.AddFee(10.00m); // Exactly at threshold

        // Act & Assert
        member.CanBorrowBooks().Should().BeTrue();
    }

    [Fact]
    public void UpdateContactInfo_UpdatesContactDetails()
    {
        // Arrange
        var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);

        // Act
        member.UpdateContactInfo("newemail@example.com", "+1-555-1234", "123 Main St");

        // Assert
        member.Email.Should().Be("newemail@example.com");
        member.PhoneNumber.Should().Be("+1-555-1234");
        member.Address.Should().Be("123 Main St");
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var member = new Member("LIB-2024-001", "John", "Doe", "john@example.com", DateTime.Today);

        // Act
        var result = member.ToString();

        // Assert
        result.Should().Contain("John Doe");
        result.Should().Contain("LIB-2024-001");
    }
}
