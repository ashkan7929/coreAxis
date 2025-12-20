using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.SharedKernel
{
    /// <summary>
    /// Represents a paginated list of items.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    public class PaginatedList<T>
    {
        /// <summary>
        /// Gets the items for the current page.
        /// </summary>
        public IReadOnlyList<T> Items { get; }

        /// <summary>
        /// Gets the current page number (1-based).
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// Gets the number of items per page.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Gets the total number of items across all pages.
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public int TotalPages { get; }

        /// <summary>
        /// Gets a value indicating whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Gets a value indicating whether there is a next page.
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaginatedList{T}"/> class.
        /// </summary>
        /// <param name="items">The items for the current page.</param>
        /// <param name="totalCount">The total number of items across all pages.</param>
        /// <param name="pageNumber">The current page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        public PaginatedList(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            Items = items.ToList().AsReadOnly();
        }

        /// <summary>
        /// Creates a paginated list from a queryable source.
        /// </summary>
        /// <param name="source">The queryable source.</param>
        /// <param name="pageNumber">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A paginated list.</returns>
        public static PaginatedList<T> Create(IQueryable<T> source, int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1)
                throw new ArgumentException("Page size must be greater than 0.", nameof(pageSize));

            var totalCount = source.Count();
            var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            return new PaginatedList<T>(items, totalCount, pageNumber, pageSize);
        }

        /// <summary>
        /// Creates a paginated list from a queryable source asynchronously.
        /// </summary>
        /// <param name="source">The queryable source.</param>
        /// <param name="pageNumber">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A paginated list.</returns>
        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1)
                throw new ArgumentException("Page size must be greater than 0.", nameof(pageSize));

            var totalCount = await source.CountAsync(cancellationToken);
            var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return new PaginatedList<T>(items, totalCount, pageNumber, pageSize);
        }

        /// <summary>
        /// Creates a paginated list from a collection.
        /// </summary>
        /// <param name="source">The collection.</param>
        /// <param name="pageNumber">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A paginated list.</returns>
        public static PaginatedList<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1)
                throw new ArgumentException("Page size must be greater than 0.", nameof(pageSize));

            var list = source.ToList();
            var totalCount = list.Count;
            var items = list.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            return new PaginatedList<T>(items, totalCount, pageNumber, pageSize);
        }

        /// <summary>
        /// Creates a paginated list with explicit items and total count.
        /// </summary>
        /// <param name="items">The items for the current page.</param>
        /// <param name="pageNumber">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="totalCount">The total number of items across all pages.</param>
        /// <returns>A paginated list.</returns>
        public static PaginatedList<T> Create(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1)
                throw new ArgumentException("Page size must be greater than 0.", nameof(pageSize));

            return new PaginatedList<T>(items, totalCount, pageNumber, pageSize);
        }
    }
}