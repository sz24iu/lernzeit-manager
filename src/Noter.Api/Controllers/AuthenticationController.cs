using Microsoft.AspNetCore.Mvc;
using Noter.Domain.Entities.Dtos.AuthenticationDto;
using Noter.Domain.Entities.Dtos.UserDto;
using Noter.Domain.Repositories;

namespace Noter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationRepository _repository;
        public AuthenticationController(IAuthenticationRepository repository)
        {
            _repository = repository;
        }

        [HttpPost]
        [Route("registration")]
        public async Task<IActionResult> Register([FromBody] CreateUserDto registrationDto)
        {
            var result = await _repository.Registration(registrationDto);

            return Ok(result);
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto LoginDto)
        {
            var result = await _repository.Login(LoginDto);

            return Ok(result);
        }

        [HttpPost]
        [Route("refresh_token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenDto tokenDto)
        {
            var result = await _repository.VerifyToken(tokenDto);

            return Ok(result);
        }
    }
}
