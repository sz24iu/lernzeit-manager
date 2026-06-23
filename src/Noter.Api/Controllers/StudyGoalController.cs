using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using Noter.Domain.Entities.Dtos.StudyGoalDto;
using Noter.Domain.Repositories;

namespace Noter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class StudyGoalController : ControllerBase
{
    private readonly IStudyGoalRepository _repository;

    public StudyGoalController(IStudyGoalRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStudyGoalById(Guid id)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _repository.GetByIdAsync(id, userId.Value);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStudyGoalDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Description))
        {
            return BadRequest(new { message = "Title and description are required." });
        }

        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        dto.UserId = userId.Value;

        await _repository.AddAsync(dto);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetAllStudyGoals()
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var result = await _repository.GetAllAsync(userId);
        return Ok(result);
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}