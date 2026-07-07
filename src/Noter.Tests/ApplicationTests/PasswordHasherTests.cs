using System;
using Xunit;
using FluentAssertions;
using Noter.Application.HashingUnits;

namespace Noter.Tests.ApplicationTests
{
    public class PasswordHasherTests
    {
        [Fact]
        public void Secure_ShouldHashPassword_NotContainCleartext()
        {
            // Arrange
            var password = "MySecurePassword123!";

            // Act
            var hash = PasswordHasher.Secure(password);

            // Assert
            hash.Should().NotBeNullOrEmpty();
            hash.Should().NotContain(password);
            hash.Should().Contain(";"); // Contains salt and hash separated by delimiter
        }

        [Fact]
        public void Secure_ShouldGenerateDifferentHashesForSamePassword()
        {
            // Arrange
            var password = "MySecurePassword123!";

            // Act
            var hash1 = PasswordHasher.Secure(password);
            var hash2 = PasswordHasher.Secure(password);

            // Assert - Different hashes due to random salt
            hash1.Should().NotBe(hash2);
        }

        [Fact]
        public void Validete_ShouldReturnTrue_WithCorrectPassword()
        {
            // Arrange
            var password = "MySecurePassword123!";
            var hash = PasswordHasher.Secure(password);

            // Act
            var result = PasswordHasher.Validete(hash, password);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Validete_ShouldReturnFalse_WithWrongPassword()
        {
            // Arrange
            var password = "MySecurePassword123!";
            var wrongPassword = "WrongPassword!";
            var hash = PasswordHasher.Secure(password);

            // Act
            var result = PasswordHasher.Validete(hash, wrongPassword);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Validete_ShouldReturnFalse_WithEmptyPassword()
        {
            // Arrange
            var password = "MySecurePassword123!";
            var hash = PasswordHasher.Secure(password);

            // Act
            var result = PasswordHasher.Validete(hash, "");

            // Assert
            result.Should().BeFalse();
        }
    }
}
