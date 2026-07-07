using System;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Noter.Api.Controllers;
using Noter.Domain.Entities.Dtos.StudyGoalDto;
using Noter.Domain.Repositories;

namespace Noter.Tests.Controllers
{
    public class StudyGoalControllerTests
    {
        private readonly Mock<IStudyGoalRepository> _mockRepository;
        private readonly StudyGoalController _controller;

        public StudyGoalControllerTests()
        {
            _mockRepository = new Mock<IStudyGoalRepository>();
            _controller = new StudyGoalController(_mockRepository.Object);
        }

        [Fact]
        public async Task Create_WithMissingTitle_ReturnsBadRequest()
        {
            // Arrange
            var createStudyGoalDto = new CreateStudyGoalDto
            {
                Title = "",
                Description = "Some description"
            };

            // Act
            var result = await _controller.Create(createStudyGoalDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest?.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_WithMissingDescription_ReturnsBadRequest()
        {
            // Arrange
            var createStudyGoalDto = new CreateStudyGoalDto
            {
                Title = "Learn C#",
                Description = ""
            };

            // Act
            var result = await _controller.Create(createStudyGoalDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
