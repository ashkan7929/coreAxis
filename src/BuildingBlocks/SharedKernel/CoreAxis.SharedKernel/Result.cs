using System;
using System.Collections.Generic;

namespace CoreAxis.SharedKernel
{
    /// <summary>
    /// Represents the result of an operation, with support for success/failure status and error messages.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    public class Result<T>
    {
        private readonly List<string> _errors = new List<string>();

        /// <summary>
        /// Gets a value indicating whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Gets the value of the result.
        /// </summary>
        public T Value { get; private set; } = default!;

        /// <summary>
        /// Gets the error messages associated with a failed operation.
        /// </summary>
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();

        /// <summary>
        /// Creates a successful result with the specified value.
        /// </summary>
        /// <param name="value">The result value.</param>
        /// <returns>A successful result.</returns>
        public static Result<T> Success(T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return new Result<T> { IsSuccess = true, Value = value };
        }

        /// <summary>
        /// Creates a failed result with the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>A failed result.</returns>
        public static Result<T> Failure(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));
            var result = new Result<T> { IsSuccess = false };
            result._errors.Add(errorMessage);
            return result;
        }

        /// <summary>
        /// Creates a failed result with the specified error messages.
        /// </summary>
        /// <param name="errors">The error messages.</param>
        /// <returns>A failed result.</returns>
        public static Result<T> Failure(IEnumerable<string> errors)
        {
            var result = new Result<T> { IsSuccess = false };
            result._errors.AddRange(errors);
            return result;
        }

        /// <summary>
        /// Implicitly converts a value to a successful result.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator Result<T>(T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return Success(value);
        }
    }

    /// <summary>
    /// Provides static methods for creating results without a value.
    /// </summary>
    public static class Result
    {
        /// <summary>
        /// Creates a successful result without a value.
        /// </summary>
        /// <returns>A successful result.</returns>
        public static Result<bool> Success()
        {
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Creates a successful result with the specified value.
        /// </summary>
        /// <typeparam name="T">The type of the result value.</typeparam>
        /// <param name="value">The result value.</param>
        /// <returns>A successful result.</returns>
        public static Result<T> Success<T>(T value)
        {
            return Result<T>.Success(value);
        }

        /// <summary>
        /// Creates a failed result with the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>A failed result.</returns>
        public static Result<bool> Failure(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));
            return Result<bool>.Failure(errorMessage);
        }

        /// <summary>
        /// Creates a failed result with the specified error message.
        /// </summary>
        /// <typeparam name="T">The type of the result value.</typeparam>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>A failed result.</returns>
        public static Result<T> Failure<T>(string errorMessage)
        {
            return Result<T>.Failure(errorMessage);
        }

        /// <summary>
        /// Creates a failed result with the specified error messages.
        /// </summary>
        /// <param name="errors">The error messages.</param>
        /// <returns>A failed result.</returns>
        public static Result<bool> Failure(IEnumerable<string> errors)
        {
            return Result<bool>.Failure(errors);
        }

        /// <summary>
        /// Creates a failed result with the specified error messages.
        /// </summary>
        /// <typeparam name="T">The type of the result value.</typeparam>
        /// <param name="errors">The error messages.</param>
        /// <returns>A failed result.</returns>
        public static Result<T> Failure<T>(IEnumerable<string> errors)
        {
            return Result<T>.Failure(errors);
        }
    }
}