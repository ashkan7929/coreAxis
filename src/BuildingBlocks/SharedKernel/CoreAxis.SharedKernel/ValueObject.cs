using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreAxis.SharedKernel
{
    /// <summary>
    /// Base class for all value objects in the system.
    /// Value objects are immutable and are compared by their structural equality.
    /// </summary>
    public abstract class ValueObject
    {
        /// <summary>
        /// Gets the atomic values that make up this value object.
        /// </summary>
        /// <returns>An enumerable of objects representing the atomic values.</returns>
        protected abstract IEnumerable<object> GetEqualityComponents();

        /// <summary>
        /// Determines whether this value object is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>true if the objects are equal; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            var other = (ValueObject)obj;

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        /// <summary>
        /// Gets the hash code for this value object.
        /// </summary>
        /// <returns>A hash code derived from the equality components.</returns>
        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x != null ? x.GetHashCode() : 0)
                .Aggregate((x, y) => x ^ y);
        }

        /// <summary>
        /// Determines whether two value objects are equal.
        /// </summary>
        /// <param name="left">The first value object.</param>
        /// <param name="right">The second value object.</param>
        /// <returns>true if the value objects are equal; otherwise, false.</returns>
        public static bool operator ==(ValueObject left, ValueObject right)
        {
            if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
                return true;

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two value objects are not equal.
        /// </summary>
        /// <param name="left">The first value object.</param>
        /// <param name="right">The second value object.</param>
        /// <returns>true if the value objects are not equal; otherwise, false.</returns>
        public static bool operator !=(ValueObject left, ValueObject right)
        {
            return !(left == right);
        }
    }
}