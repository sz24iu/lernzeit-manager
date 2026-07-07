using System;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Noter.Application.HashingUnits;
using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Dtos.AuthenticationDto;
using Noter.Domain.Entities.Dtos.UserDto;
using Noter.Inrastructure.Persistence.DbContexts;

namespace Noter.Tests.IntegrationTests
{
    public class AuthenticationIntegrationTests
    {
        private NoterDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<NoterDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new NoterDbContext(options);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnSuccess()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var testPassword = "TestPassword123";
            var hashedPassword = PasswordHasher.Secure(testPassword);
            
            var userDto = new CreateUserDto
            {
                Email = "test@example.com",
                HashPassword = hashedPassword
            };

            var user = new User(userDto);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var matchesPassword = PasswordHasher.Validete(user.HashPassword, testPassword);

            // Assert
            matchesPassword.Should().BeTrue();
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnFailure()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var testPassword = "TestPassword123";
            var hashedPassword = PasswordHasher.Secure(testPassword);

            var userDto = new CreateUserDto
            {
                Email = "test@example.com",
                HashPassword = hashedPassword
            };

            var user = new User(userDto);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var wrongPassword = "WrongPassword123";

            // Act
            var matchesPassword = PasswordHasher.Validete(user.HashPassword, wrongPassword);

            // Assert
            matchesPassword.Should().BeFalse();
        }

        [Fact]
        public async Task Database_ShouldStoreUserWithHashedPassword()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var testPassword = "SecurePassword456";
            var hashedPassword = PasswordHasher.Secure(testPassword);

            var userDto = new CreateUserDto
            {
                Email = "newuser@example.com",
                HashPassword = hashedPassword
            };

            var user = new User(userDto);

            // Act
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Assert
            var retrievedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "newuser@example.com");
            retrievedUser.Should().NotBeNull();
            retrievedUser?.HashPassword.Should().NotBe(testPassword);
            retrievedUser?.HashPassword.Should().Be(hashedPassword);
        }
    }
}
