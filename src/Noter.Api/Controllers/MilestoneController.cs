using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

            try
            {
                var result = await _repository.GetByStudyGoalIdAsync(id, userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Problem(
                    detail: ex.Message,
                    title: "Meilensteine konnten nicht geladen werden.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddMilestone(CreateMilestoneDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return BadRequest(new { message = "Milestone title is required." });
            }

            if (dto.StartDateTime >= dto.EndDateTime)
            {
                return BadRequest(new { message = "Milestone start time must be before end time." });
            }

            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            await _repository.AddAsync(dto, userId.Value);
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

        [HttpPost("{id}/track")]
        public async Task<IActionResult> TrackMilestoneStudyTime(Guid id, [FromBody] TrackMilestoneStudyTimeDto dto)
        {
            if (dto.TrackedMinutes <= 0)
            {
                return BadRequest(new { message = "Tracked minutes must be greater than zero." });
            }

            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            await _repository.AddTrackedSessionAsync(id, dto.TrackedMinutes, userId.Value);
            return Ok();
        }

        private Guid? GetUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("Id")?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(userIdValue, out var userId) ? userId : null;
        }

    }
}
