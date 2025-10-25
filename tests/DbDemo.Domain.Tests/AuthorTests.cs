using Xunit;
using DbDemo.Models;
using FluentAssertions;

namespace DbDemo.Domain.Tests;

public class AuthorTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesAuthor()
    {
        // Act
        var author = new Author("Robert", "Martin");

        // Assert
        author.FirstName.Should().Be("Robert");
        author.LastName.Should().Be("Martin");
        author.FullName.Should().Be("Robert Martin");
        author.Email.Should().BeNull();
        author.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithEmail_CreatesAuthor()
    {
        // Act
        var author = new Author("Robert", "Martin", "uncle.bob@example.com");

        // Assert
        author.Email.Should().Be("uncle.bob@example.com");
    }

    [Theory]
    [InlineData(null, "Martin")]
    [InlineData("", "Martin")]
    [InlineData("   ", "Martin")]
    [InlineData("Robert", null)]
    [InlineData("Robert", "")]
    [InlineData("Robert", "   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? firstName, string? lastName)
    {
        // Act
        Action act = () => new Author(firstName!, lastName!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('A', 51);

        // Act
        Action act = () => new Author(longName, "Martin");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*First name cannot exceed 50 characters*");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user")]
    public void Constructor_WithInvalidEmail_ThrowsArgumentException(string invalidEmail)
    {
        // Act
        Action act = () => new Author("Robert", "Martin", invalidEmail);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email format*");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Act
        var author = new Author("  Robert  ", "  Martin  ");

        // Assert
        author.FirstName.Should().Be("Robert");
        author.LastName.Should().Be("Martin");
    }

    [Fact]
    public void FullName_CombinesFirstAndLastName()
    {
        // Arrange
        var author = new Author("Robert", "C. Martin");

        // Act & Assert
        author.FullName.Should().Be("Robert C. Martin");
    }

    [Fact]
    public void Age_WhenDateOfBirthNotSet_ReturnsZero()
    {
        // Arrange
        var author = new Author("Robert", "Martin");

        // Act & Assert
        author.Age.Should().Be(0);
    }

    [Fact]
    public void Age_CalculatesCorrectAge()
    {
        // Arrange
        var author = new Author("Robert", "Martin");
        var dateOfBirth = DateTime.Today.AddYears(-50);

        // Act
        author.UpdateBiography("Author bio", dateOfBirth);

        // Assert
        author.Age.Should().Be(50);
    }

    [Fact]
    public void Age_BeforeBirthday_CalculatesCorrectAge()
    {
        // Arrange
        var author = new Author("Robert", "Martin");
        var dateOfBirth = DateTime.Today.AddYears(-50).AddDays(1); // Birthday tomorrow

        // Act
        author.UpdateBiography("Author bio", dateOfBirth);

        // Assert
        author.Age.Should().Be(49); // Not yet 50
    }

    [Fact]
    public void UpdateDetails_WithValidData_UpdatesAuthor()
    {
        // Arrange
        var author = new Author("Robert", "Martin");
        var originalUpdatedAt = author.UpdatedAt;
        Thread.Sleep(10);

        // Act
        author.UpdateDetails("Bob", "C. Martin", "bob@example.com");

        // Assert
        author.FirstName.Should().Be("Bob");
        author.LastName.Should().Be("C. Martin");
        author.Email.Should().Be("bob@example.com");
        author.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateDetails_WithInvalidEmail_ThrowsArgumentException()
    {
        // Arrange
        var author = new Author("Robert", "Martin");

        // Act
        Action act = () => author.UpdateDetails("Robert", "Martin", "invalid-email");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email format*");
    }

    [Fact]
    public void UpdateBiography_UpdatesAuthorBiography()
    {
        // Arrange
        var author = new Author("Robert", "Martin");
        var dateOfBirth = new DateTime(1952, 12, 5);

        // Act
        author.UpdateBiography("Software craftsman", dateOfBirth, "American");

        // Assert
        author.Biography.Should().Be("Software craftsman");
        author.DateOfBirth.Should().Be(dateOfBirth);
        author.Nationality.Should().Be("American");
    }

    [Fact]
    public void ToString_ReturnsFullName()
    {
        // Arrange
        var author = new Author("Robert", "C. Martin");

        // Act
        var result = author.ToString();

        // Assert
        result.Should().Be("Robert C. Martin");
    }
}
