using CurrencyConverterDemo.Domain.Models;
using FluentAssertions;

namespace CurrencyConverterDemo.Tests.Unit.Domain;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_WithExactDivision_ReturnsCorrectValue()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = ["item1", "item2", "item3", "item4", "item5"],
            Page = 1,
            PageSize = 5,
            TotalCount = 15
        };

        // Assert
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void TotalPages_WithRemainder_RoundsUp()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = ["item1", "item2", "item3"],
            Page = 1,
            PageSize = 3,
            TotalCount = 10
        };

        // Assert
        result.TotalPages.Should().Be(4); // 10 / 3 = 3.33 -> rounds up to 4
    }

    [Fact]
    public void TotalPages_WithEmptyResult_ReturnsZero()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = [],
            Page = 1,
            PageSize = 10,
            TotalCount = 0
        };

        // Assert
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public void TotalPages_WithSingleItem_ReturnsOne()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = ["item1"],
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        // Assert
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public void HasNextPage_OnFirstPageOfMultiple_ReturnsTrue()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = ["item1", "item2"],
            Page = 1,
            PageSize = 2,
            TotalCount = 10
        };

        // Assert
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_OnLastPage_ReturnsFalse()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = ["item9", "item10"],
            Page = 5,
            PageSize = 2,
            TotalCount = 10
        };

        // Assert
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasNextPage_OnOnlyPage_ReturnsFalse()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = ["item1", "item2"],
            Page = 1,
            PageSize = 10,
            TotalCount = 2
        };

        // Assert
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_OnFirstPage_ReturnsFalse()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = ["item1", "item2"],
            Page = 1,
            PageSize = 2,
            TotalCount = 10
        };

        // Assert
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_OnSecondPage_ReturnsTrue()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = ["item3", "item4"],
            Page = 2,
            PageSize = 2,
            TotalCount = 10
        };

        // Assert
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void HasPreviousPage_OnLastPage_ReturnsTrue()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = ["item9", "item10"],
            Page = 5,
            PageSize = 2,
            TotalCount = 10
        };

        // Assert
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_WithDifferentType_WorksCorrectly()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            Items = [1, 2, 3],
            Page = 2,
            PageSize = 3,
            TotalCount = 10
        };

        // Assert
        result.Items.Should().Equal(1, 2, 3);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(3);
        result.TotalCount.Should().Be(10);
        result.TotalPages.Should().Be(4);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_ExactPageBoundary_WorksCorrectly()
    {
        // Arrange - 20 items, page size 5 = exactly 4 pages
        var result = new PagedResult<string>
        {
            Items = ["item16", "item17", "item18", "item19", "item20"],
            Page = 4,
            PageSize = 5,
            TotalCount = 20
        };

        // Assert
        result.TotalPages.Should().Be(4);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }
}
