using System;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Noter.Api.Controllers;
using Noter.Domain.Entities.ConfigEntities;
using Noter.Domain.Entities.Dtos;
using Noter.Domain.Entities.Dtos.AuthenticationDto;
using Noter.Domain.Entities.Dtos.UserDto;
using Noter.Domain.Repositories;

namespace Noter.Tests.Controllers
{
    public class AuthenticationControllerTests
    {
        private readonly Mock<IAuthenticationRepository> _mockAuthRepository;
        private readonly AuthenticationController _controller;

        public AuthenticationControllerTests()
        {
            _mockAuthRepository = new Mock<IAuthenticationRepository>();
            _controller = new AuthenticationController(_mockAuthRepository.Object);
        }

        [Fact]
        public async Task Register_WithValidData_ReturnsOk()
        {
            // Arrange
            var registrationDto = new CreateUserDto
            {
                Email = "test@example.com",
                HashPassword = "hashedpassword"
            };

            var mockResult = new AuthResult { Success = true };
            _mockAuthRepository.Setup(x => x.Registration(It.IsAny<CreateUserDto>()))
                .ReturnsAsync(mockResult);

            // Act
            var result = await _controller.Register(registrationDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.Value.Should().Be(mockResult);
            _mockAuthRepository.Verify(x => x.Registration(registrationDto), Times.Once);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                Email = "test@example.com",
                Password = "password123"
            };

            var mockAuthResult = new AuthResult
            {
                Success = true,
                Token = "jwt-token-123",
                RefreshToken = "refresh-token-123"
            };

            _mockAuthRepository.Setup(x => x.Login(It.IsAny<UserLoginDto>()))
                .ReturnsAsync(mockAuthResult);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var returnedValue = okResult?.Value as AuthResult;
            returnedValue?.Success.Should().BeTrue();
            returnedValue?.Token.Should().Be("jwt-token-123");
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsOkWithError()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                Email = "test@example.com",
                Password = "wrongpassword"
            };

            var mockAuthResult = new AuthResult
            {
                Success = false,
                Errors = new List<string> { "Invalid credentials" }
            };

            _mockAuthRepository.Setup(x => x.Login(It.IsAny<UserLoginDto>()))
                .ReturnsAsync(mockAuthResult);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var returnedValue = okResult?.Value as AuthResult;
            returnedValue?.Success.Should().BeFalse();
            returnedValue?.Errors.Should().Contain("Invalid credentials");
        }

        [Fact]
        public async Task RefreshToken_WithValidToken_ReturnsOk()
        {
            // Arrange
            var tokenDto = new TokenDto
            {
                Token = "old-jwt-token",
                RefreshToken = "refresh-token-123"
            };

            var mockAuthResult = new AuthResult
            {
                Success = true,
                Token = "new-jwt-token"
            };

            _mockAuthRepository.Setup(x => x.VerifyToken(It.IsAny<TokenDto>()))
                .ReturnsAsync(mockAuthResult);

            // Act
            var result = await _controller.RefreshToken(tokenDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var returnedValue = okResult?.Value as AuthResult;
            returnedValue?.Success.Should().BeTrue();
        }
    }
}
