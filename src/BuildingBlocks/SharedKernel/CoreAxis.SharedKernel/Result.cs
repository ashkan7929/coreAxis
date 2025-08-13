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
            return new Result<T> { IsSuccess = true, Value = value };
        }

        /// <summary>
        /// Creates a failed result with the specified error message.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <returns>A failed result.</returns>
        public static Result<T> Failure(string error)
        {
            var result = new Result<T> { IsSuccess = false };
            result._errors.Add(error);
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
        /// Creates a failed result with the specified error message.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <returns>A failed result.</returns>
        public static Result<bool> Failure(string error)
        {
            return Result<bool>.Failure(error);
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
    }
}