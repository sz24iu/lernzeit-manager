using Noter.Domain.Entities.Dtos;
using Noter.Domain.Entities.Dtos.AuthenticationDto;
using Noter.Domain.Entities.Dtos.UserDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Repositories
{
    public interface IAuthenticationRepository
    {
        Task<AuthResult> Registration(CreateUserDto registrationDto);
        Task<AuthResult> Login(UserLoginDto loginDto);
        Task<AuthResult> VerifyToken(TokenDto requestDto);
    }
}
