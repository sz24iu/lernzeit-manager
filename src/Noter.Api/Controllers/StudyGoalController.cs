using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _repository.GetAllAsync(userId.Value);
        return Ok(result);
    }

    [HttpGet("completed")]
    public async Task<IActionResult> GetCompletedStudyGoals()
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _repository.GetCompletedAsync(userId.Value);
        return Ok(result);
    }

    [HttpDelete("{id}/complete")]
    public async Task<IActionResult> CompleteAndRemoveStudyGoal(Guid id)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            await _repository.CompleteAndRemoveAsync(id, userId.Value);
            return Ok(new { message = "Lernziel wurde abgeschlossen." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("Id")?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}