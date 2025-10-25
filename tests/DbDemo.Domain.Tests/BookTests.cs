using Xunit;
using DbDemo.Models;
using FluentAssertions;

namespace DbDemo.Domain.Tests;

public class BookTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesBook()
    {
        // Act
        var book = new Book("978-0-13-468599-1", "Clean Code", categoryId: 1, totalCopies: 5);

        // Assert
        book.ISBN.Should().Be("978-0-13-468599-1");
        book.Title.Should().Be("Clean Code");
        book.CategoryId.Should().Be(1);
        book.TotalCopies.Should().Be(5);
        book.AvailableCopies.Should().Be(5);
        book.IsDeleted.Should().BeFalse();
        book.IsAvailable.Should().BeTrue();
        book.CopiesOnLoan.Should().Be(0);
    }

    [Theory]
    [InlineData("0134685997")]  // ISBN-10
    [InlineData("9780134685991")] // ISBN-13
    [InlineData("978-0-13-468599-1")] // ISBN-13 with hyphens
    [InlineData("0-13-468599-7")] // ISBN-10 with hyphens
    public void Constructor_WithValidISBN_CreatesBook(string isbn)
    {
        // Act
        var book = new Book(isbn, "Clean Code", 1, 5);

        // Assert
        book.ISBN.Should().Be(isbn);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidISBN_ThrowsArgumentException(string? invalidIsbn)
    {
        // Act
        Action act = () => new Book(invalidIsbn!, "Clean Code", 1, 5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ISBN cannot be empty*");
    }

    [Theory]
    [InlineData("123")]  // Too short
    [InlineData("12345678901234")]  // Too long
    [InlineData("ABCDEFGHIJ")]  // Not digits
    public void Constructor_WithInvalidISBNFormat_ThrowsArgumentException(string invalidIsbn)
    {
        // Act
        Action act = () => new Book(invalidIsbn, "Clean Code", 1, 5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid ISBN format*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTitle_ThrowsArgumentException(string? invalidTitle)
    {
        // Act
        Action act = () => new Book("978-0-13-468599-1", invalidTitle!, 1, 5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Title cannot be empty*");
    }

    [Fact]
    public void Constructor_WithTitleTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longTitle = new string('A', 201);

        // Act
        Action act = () => new Book("978-0-13-468599-1", longTitle, 1, 5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Title cannot exceed 200 characters*");
    }

    [Fact]
    public void BorrowCopy_WhenAvailable_DecreasesAvailableCopies()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);

        // Act
        book.BorrowCopy();

        // Assert
        book.AvailableCopies.Should().Be(4);
        book.CopiesOnLoan.Should().Be(1);
    }

    [Fact]
    public void BorrowCopy_WhenNoAvailableCopies_ThrowsInvalidOperationException()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 1);
        book.BorrowCopy();

        // Act
        Action act = () => book.BorrowCopy();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Book is not available for borrowing*");
    }

    [Fact]
    public void BorrowCopy_WhenDeleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);
        book.MarkAsDeleted();

        // Act
        Action act = () => book.BorrowCopy();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReturnCopy_IncreasesAvailableCopies()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);
        book.BorrowCopy();

        // Act
        book.ReturnCopy();

        // Assert
        book.AvailableCopies.Should().Be(5);
        book.CopiesOnLoan.Should().Be(0);
    }

    [Fact]
    public void ReturnCopy_WhenAllCopiesAvailable_ThrowsInvalidOperationException()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);

        // Act
        Action act = () => book.ReturnCopy();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot return more copies than total*");
    }

    [Fact]
    public void AddCopies_IncreasesTotalAndAvailableCopies()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);

        // Act
        book.AddCopies(3);

        // Assert
        book.TotalCopies.Should().Be(8);
        book.AvailableCopies.Should().Be(8);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddCopies_WithInvalidCount_ThrowsArgumentException(int invalidCount)
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);

        // Act
        Action act = () => book.AddCopies(invalidCount);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Count must be positive*");
    }

    [Fact]
    public void MarkAsDeleted_WhenNoCopiesOnLoan_MarksAsDeleted()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);

        // Act
        book.MarkAsDeleted();

        // Assert
        book.IsDeleted.Should().BeTrue();
        book.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void MarkAsDeleted_WhenCopiesOnLoan_ThrowsInvalidOperationException()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);
        book.BorrowCopy();

        // Act
        Action act = () => book.MarkAsDeleted();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot delete book with copies on loan*");
    }

    [Fact]
    public void IsAvailable_WhenCopiesAvailableAndNotDeleted_ReturnsTrue()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);

        // Act & Assert
        book.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_WhenNoCopiesAvailable_ReturnsFalse()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 1);
        book.BorrowCopy();

        // Act & Assert
        book.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_WhenDeleted_ReturnsFalse()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);
        book.MarkAsDeleted();

        // Act & Assert
        book.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void UpdateDetails_UpdatesBookInformation()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);

        // Act
        book.UpdateDetails("Clean Code: A Handbook", "Agile Software Craftsmanship",
            "A guide to writing clean code", "Prentice Hall");

        // Assert
        book.Title.Should().Be("Clean Code: A Handbook");
        book.Subtitle.Should().Be("Agile Software Craftsmanship");
        book.Description.Should().Be("A guide to writing clean code");
        book.Publisher.Should().Be("Prentice Hall");
    }

    [Fact]
    public void UpdatePublishingInfo_UpdatesPublishingDetails()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);
        var publishedDate = new DateTime(2008, 8, 1);

        // Act
        book.UpdatePublishingInfo(publishedDate, 464, "English");

        // Assert
        book.PublishedDate.Should().Be(publishedDate);
        book.PageCount.Should().Be(464);
        book.Language.Should().Be("English");
    }

    [Fact]
    public void UpdateShelfLocation_UpdatesLocation()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);

        // Act
        book.UpdateShelfLocation("A-42");

        // Assert
        book.ShelfLocation.Should().Be("A-42");
    }

    [Fact]
    public void CopiesOnLoan_CalculatesCorrectly()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);

        // Act
        book.BorrowCopy();
        book.BorrowCopy();

        // Assert
        book.CopiesOnLoan.Should().Be(2);
        book.AvailableCopies.Should().Be(3);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);

        // Act
        var result = book.ToString();

        // Assert
        result.Should().Contain("Clean Code");
        result.Should().Contain("978-0-13-468599-1");
    }
}
