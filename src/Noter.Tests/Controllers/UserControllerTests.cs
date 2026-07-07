using System;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Noter.Api.Controllers;
using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Dtos.UserDto;
using Noter.Domain.Repositories;

namespace Noter.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserRepository> _mockRepository;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _mockRepository = new Mock<IUserRepository>();
            _controller = new UserController(_mockRepository.Object);
        }

        [Fact]
        public async Task GetUserById_WithValidId_ReturnsOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userDto = new CreateUserDto
            {
                Email = "test@example.com",
                HashPassword = "hashed"
            };
            var mockUser = new User(userDto);

            _mockRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(mockUser);

            // Act
            var result = await _controller.GetUserById(userId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.Value.Should().Be(mockUser);
            _mockRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task Create_WithValidUser_ReturnsOk()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Email = "newuser@example.com",
                HashPassword = "hashed_password"
            };

            _mockRepository.Setup(x => x.AddAsync(It.IsAny<CreateUserDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(createUserDto);

            // Assert
            result.Should().BeOfType<OkResult>();
            _mockRepository.Verify(x => x.AddAsync(createUserDto), Times.Once);
        }
    }
}
