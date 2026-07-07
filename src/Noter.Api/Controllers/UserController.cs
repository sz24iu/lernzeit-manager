using Microsoft.AspNetCore.Mvc;
using Noter.Domain.Entities.Dtos.StudyGoalDto;
using Noter.Domain.Entities.Dtos.UserDto;
using Noter.Domain.Repositories;

namespace Noter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _repository;

        public UserController(IUserRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var result = await _repository.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserDto dto)
        {
            await _repository.AddAsync(dto);
            return Ok();
        }
    }
}
