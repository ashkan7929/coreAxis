using CoreAxis.SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CoreAxis.Tests.SharedKernel
{
    /// <summary>
    /// Unit tests for the PaginatedList class.
    /// </summary>
    public class PaginatedListTests
    {
        /// <summary>
        /// Tests that Create from IQueryable returns a paginated list with the correct items and pagination info.
        /// </summary>
        [Fact]
        public void Create_FromIQueryable_ShouldReturnPaginatedList()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var queryable = items.AsQueryable();
            int pageNumber = 2;
            int pageSize = 3;

            // Act
            var result = PaginatedList<int>.Create(queryable, pageNumber, pageSize);

            // Assert
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(4, result.Items[0]); // First item on page 2 should be 4
            Assert.Equal(5, result.Items[1]);
            Assert.Equal(6, result.Items[2]);
            Assert.Equal(pageNumber, result.PageNumber);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(10, result.TotalCount);
            Assert.Equal(4, result.TotalPages);
        }

        /// <summary>
        /// Tests that Create from IEnumerable returns a paginated list with the correct items and pagination info.
        /// </summary>
        [Fact]
        public void Create_FromIEnumerable_ShouldReturnPaginatedList()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            int pageNumber = 2;
            int pageSize = 3;

            // Act
            var result = PaginatedList<int>.Create(items, pageNumber, pageSize);

            // Assert
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(4, result.Items[0]); // First item on page 2 should be 4
            Assert.Equal(5, result.Items[1]);
            Assert.Equal(6, result.Items[2]);
            Assert.Equal(pageNumber, result.PageNumber);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(10, result.TotalCount);
            Assert.Equal(4, result.TotalPages);
        }

        /// <summary>
        /// Tests that Create with explicit items and count returns a paginated list with the correct items and pagination info.
        /// </summary>
        [Fact]
        public void Create_WithItemsAndCount_ShouldReturnPaginatedList()
        {
            // Arrange
            var items = new List<int> { 4, 5, 6 }; // Items for page 2
            int pageNumber = 2;
            int pageSize = 3;
            int totalCount = 10;

            // Act
            var result = PaginatedList<int>.Create(items, pageNumber, pageSize, totalCount);

            // Assert
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(4, result.Items[0]);
            Assert.Equal(5, result.Items[1]);
            Assert.Equal(6, result.Items[2]);
            Assert.Equal(pageNumber, result.PageNumber);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(totalCount, result.TotalCount);
            Assert.Equal(4, result.TotalPages);
        }

        /// <summary>
        /// Tests that Create with a page number less than 1 throws an ArgumentException.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithPageNumberLessThan1_ShouldThrowArgumentException(int pageNumber)
        {
            // Arrange
            var items = new List<int> { 1, 2, 3, 4, 5 };
            int pageSize = 2;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => PaginatedList<int>.Create(items, pageNumber, pageSize));
            Assert.Equal("pageNumber", exception.ParamName);
        }

        /// <summary>
        /// Tests that Create with a page size less than 1 throws an ArgumentException.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithPageSizeLessThan1_ShouldThrowArgumentException(int pageSize)
        {
            // Arrange
            var items = new List<int> { 1, 2, 3, 4, 5 };
            int pageNumber = 1;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => PaginatedList<int>.Create(items, pageNumber, pageSize));
            Assert.Equal("pageSize", exception.ParamName);
        }

        /// <summary>
        /// Tests that HasPreviousPage returns the correct value based on the page number.
        /// </summary>
        [Theory]
        [InlineData(1, false)]
        [InlineData(2, true)]
        public void HasPreviousPage_ShouldReturnCorrectValue(int pageNumber, bool expectedResult)
        {
            // Arrange
            var items = new List<int> { 1, 2, 3 };
            int pageSize = 3;
            int totalCount = 10;

            // Act
            var paginatedList = PaginatedList<int>.Create(items, pageNumber, pageSize, totalCount);

            // Assert
            Assert.Equal(expectedResult, paginatedList.HasPreviousPage);
        }

        /// <summary>
        /// Tests that HasNextPage returns the correct value based on the page number and total pages.
        /// </summary>
        [Theory]
        [InlineData(1, 3, true)]
        [InlineData(3, 3, false)]
        [InlineData(4, 3, false)]
        public void HasNextPage_ShouldReturnCorrectValue(int pageNumber, int totalPages, bool expectedResult)
        {
            // Arrange
            var items = new List<int> { 1, 2, 3 };
            int pageSize = 3;
            int totalCount = totalPages * pageSize;

            // Act
            var paginatedList = PaginatedList<int>.Create(items, pageNumber, pageSize, totalCount);

            // Assert
            Assert.Equal(expectedResult, paginatedList.HasNextPage);
        }

        /// <summary>
        /// Tests that TotalPages is calculated correctly based on the total count and page size.
        /// </summary>
        [Theory]
        [InlineData(10, 3, 4)] // 10 items with page size 3 should be 4 pages
        [InlineData(10, 5, 2)] // 10 items with page size 5 should be 2 pages
        [InlineData(10, 10, 1)] // 10 items with page size 10 should be 1 page
        [InlineData(10, 15, 1)] // 10 items with page size 15 should be 1 page
        [InlineData(0, 5, 0)] // 0 items should be 0 pages
        public void TotalPages_ShouldBeCalculatedCorrectly(int totalCount, int pageSize, int expectedTotalPages)
        {
            // Arrange
            var items = new List<int>();
            int pageNumber = 1;

            // Act
            var paginatedList = PaginatedList<int>.Create(items, pageNumber, pageSize, totalCount);

            // Assert
            Assert.Equal(expectedTotalPages, paginatedList.TotalPages);
        }
    }
}