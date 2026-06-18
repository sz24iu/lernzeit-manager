using Microsoft.AspNetCore.Mvc;
using Noter.Domain.Entities.Dtos.MilestoneDto;
using Noter.Domain.Repositories;

namespace Noter.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class MilestoneController : ControllerBase
    {
        private readonly IMilestoneRepository _repository;

        public MilestoneController(IMilestoneRepository repository)
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
        public async Task<IActionResult> AddMilestone(CreateMilestoneDto dto)
        {
            await _repository.AddAsync(dto);
            return Ok();
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateMilestone(UpdateMilestoneStatusDto dto)
        {
            await _repository.UpdateAsync(dto);
            return Ok();
        }

    }
}
