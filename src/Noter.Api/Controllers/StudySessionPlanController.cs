using Microsoft.AspNetCore.Mvc;
using Noter.Domain.Entities.Dtos.MilestoneDto;
using Noter.Domain.Entities.Dtos.StudySessionPlan;
using Noter.Domain.Repositories;

namespace Noter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudySessionPlanController : ControllerBase
    {
        private readonly IStudySessionPlanRepository _repository;

        public StudySessionPlanController(IStudySessionPlanRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMilestoneByStudyGoalId(Guid id)
        {
            var result = await _repository.GetByStudyGoalIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddCreateStudySessionPlan(CreateStudySessionPlanDto dto)
        {
            await _repository.AddAsync(dto);
            return Ok();
        }
    }
}
