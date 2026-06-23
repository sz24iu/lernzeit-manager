using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using Noter.Domain.Entities.Dtos.MilestoneDto;
using Noter.Domain.Repositories;

namespace Noter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var result = await _repository.GetByStudyGoalIdAsync(id, userId.Value);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddMilestone(CreateMilestoneDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return BadRequest(new { message = "Milestone title is required." });
            }

            var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            await _repository.AddAsync(dto, userId);
            return Ok();
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateMilestone(UpdateMilestoneStatusDto dto)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            await _repository.UpdateAsync(dto, userId.Value);
            return Ok();
        }

        private Guid? GetUserId()
        {
            var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(userIdValue, out var userId) ? userId : null;
        }

    }
}
