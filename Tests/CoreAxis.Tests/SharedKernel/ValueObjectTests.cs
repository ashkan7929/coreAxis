using CoreAxis.SharedKernel;
using System.Collections.Generic;
using Xunit;

namespace CoreAxis.Tests.SharedKernel
{
    /// <summary>
    /// Unit tests for the ValueObject class.
    /// </summary>
    public class ValueObjectTests
    {
        /// <summary>
        /// Tests that two value objects with the same values are equal.
        /// </summary>
        [Fact]
        public void Equals_WithSameValues_ShouldReturnTrue()
        {
            // Arrange
            var address1 = new Address("123 Main St", "Anytown", "CA", "12345");
            var address2 = new Address("123 Main St", "Anytown", "CA", "12345");

            // Act & Assert
            Assert.Equal(address1, address2);
            Assert.True(address1.Equals(address2));
            Assert.True(address1 == address2);
            Assert.False(address1 != address2);
        }

        /// <summary>
        /// Tests that two value objects with different values are not equal.
        /// </summary>
        [Fact]
        public void Equals_WithDifferentValues_ShouldReturnFalse()
        {
            // Arrange
            var address1 = new Address("123 Main St", "Anytown", "CA", "12345");
            var address2 = new Address("456 Oak Ave", "Othertown", "NY", "67890");

            // Act & Assert
            Assert.NotEqual(address1, address2);
            Assert.False(address1.Equals(address2));
            Assert.False(address1 == address2);
            Assert.True(address1 != address2);
        }

        /// <summary>
        /// Tests that a value object is not equal to null.
        /// </summary>
        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            // Arrange
            var address = new Address("123 Main St", "Anytown", "CA", "12345");

            // Act & Assert
            Assert.False(address.Equals(null));
            Assert.False(address == null);
            Assert.True(address != null);
        }

        /// <summary>
        /// Tests that a value object is not equal to an object of a different type.
        /// </summary>
        [Fact]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            // Arrange
            var address = new Address("123 Main St", "Anytown", "CA", "12345");
            var otherObject = new object();

            // Act & Assert
            Assert.False(address.Equals(otherObject));
        }

        /// <summary>
        /// Tests that two value objects with the same values have the same hash code.
        /// </summary>
        [Fact]
        public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
        {
            // Arrange
            var address1 = new Address("123 Main St", "Anytown", "CA", "12345");
            var address2 = new Address("123 Main St", "Anytown", "CA", "12345");

            // Act & Assert
            Assert.Equal(address1.GetHashCode(), address2.GetHashCode());
        }

        /// <summary>
        /// Tests that two value objects with different values have different hash codes.
        /// </summary>
        [Fact]
        public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCode()
        {
            // Arrange
            var address1 = new Address("123 Main St", "Anytown", "CA", "12345");
            var address2 = new Address("456 Oak Ave", "Othertown", "NY", "67890");

            // Act & Assert
            Assert.NotEqual(address1.GetHashCode(), address2.GetHashCode());
        }

        /// <summary>
        /// Tests that two value objects with the same values but different order of components are not equal.
        /// </summary>
        [Fact]
        public void Equals_WithSameValuesButDifferentOrder_ShouldReturnFalse()
        {
            // Arrange
            var coordinates1 = new Coordinates(10, 20);
            var coordinates2 = new Coordinates(20, 10);

            // Act & Assert
            Assert.NotEqual(coordinates1, coordinates2);
            Assert.False(coordinates1.Equals(coordinates2));
            Assert.False(coordinates1 == coordinates2);
            Assert.True(coordinates1 != coordinates2);
        }
    }

    /// <summary>
    /// Example value object representing an address.
    /// </summary>
    public class Address : ValueObject
    {
        public string Street { get; }
        public string City { get; }
        public string State { get; }
        public string ZipCode { get; }

        public Address(string street, string city, string state, string zipCode)
        {
            Street = street;
            City = city;
            State = state;
            ZipCode = zipCode;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return State;
            yield return ZipCode;
        }
    }

    /// <summary>
    /// Example value object representing coordinates.
    /// </summary>
    public class Coordinates : ValueObject
    {
        public int X { get; }
        public int Y { get; }

        public Coordinates(int x, int y)
        {
            X = x;
            Y = y;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return X;
            yield return Y;
        }
    }
}