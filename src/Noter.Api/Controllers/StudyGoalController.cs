using Microsoft.AspNetCore.Mvc;
using Noter.Domain.Entities.Dtos.StudyGoalDto;
using Noter.Domain.Repositories;

namespace Noter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        var result = await _repository.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStudyGoalDto dto)
    {
        await _repository.AddAsync(dto);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetAllStudyGoals()
    {
        var result = await _repository.GetAllsync();
        return Ok(result);
    }
}