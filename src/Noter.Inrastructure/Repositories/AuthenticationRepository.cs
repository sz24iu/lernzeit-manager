using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Noter.Application.HashingUnits;
using Noter.Domain.Entities.ConfigEntities;
using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Dtos;
using Noter.Domain.Entities.Dtos.AuthenticationDto;
using Noter.Domain.Entities.Dtos.UserDto;
using Noter.Domain.Repositories;
using Noter.Inrastructure.Persistence.DbContexts;

namespace Noter.Inrastructure.Repositories
{
    public class AuthenticationRepository : IAuthenticationRepository
    {
        private readonly NoterDbContext _context;
        private readonly JwtConfig _jwtConfig;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public AuthenticationRepository(
        NoterDbContext context,
        IOptionsMonitor<JwtConfig> optionsMonitor,
        TokenValidationParameters tokenValidationParameters)
        {
            _context = context;
            _jwtConfig = optionsMonitor.CurrentValue;
            _tokenValidationParameters = tokenValidationParameters;
        }

        public Task<AuthResult> Login(UserLoginDto loginDto)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email == loginDto.Email);

            try
            {
                if (user == null)
                    throw new Exception("Password does not found");

                if (!PasswordHasher.Validete(user.HashPassword, loginDto.Password))
                    throw new Exception("Wrong Password");
            }
            catch (Exception ex)
            {
                return Task.FromResult(new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>() {
                    ex.Message
                }
                });
            }

            var token = TokenRepository.GenerateJwtToken(user, _jwtConfig, _context);

            var userLoginResponseDto = new AuthResult()
            {
                Success = true,
                Token = token.Result.JwtToken,
                RefreshToken = token.Result.RefreshToken
            };

            return Task.FromResult(userLoginResponseDto);
        }

        public Task<AuthResult> Registration(CreateUserDto registrationDto)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email.Equals(registrationDto.Email));

            if (user != null)
            {
                return Task.FromResult(new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>() {
                "Email already in use"
                }
                });
            }

            var newHashPassword = PasswordHasher.Secure(registrationDto.HashPassword);

            registrationDto.HashPassword = newHashPassword;

            var newUser = new User(registrationDto);

            _context.Users.Add(newUser);
            _context.SaveChanges();

            var token = TokenRepository.GenerateJwtToken(newUser, _jwtConfig, _context);

            return Task.FromResult(new AuthResult()
            {
                Success = true,
                Token = token.Result.JwtToken,
                RefreshToken = token.Result.RefreshToken
            });
        }

        public async Task<AuthResult> VerifyToken(TokenDto requestDto)
        {
            var verifiedStatusOfToken =
                await TokenRepository.VerifyToken(requestDto, _tokenValidationParameters, _context);

            if (!string.IsNullOrEmpty(verifiedStatusOfToken))
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = new List<string>
            {
                verifiedStatusOfToken
            }
                };
            }

            var dbUser = await _context.RefreshTokens
                .Where(t => t.Token == requestDto.RefreshToken)
                .Join(_context.Users,
                    t => t.UserId,
                    u => u.Id,
                    (t, u) => u)
                .FirstOrDefaultAsync();

            if (dbUser == null)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = new List<string>
            {
                "User By Token Not Found"
            }
                };
            }

            var token = await TokenRepository.GenerateJwtToken(dbUser, _jwtConfig, _context);

            return new AuthResult
            {
                Success = true,
                Token = token.JwtToken,
                RefreshToken = token.RefreshToken
            };
        }
    }
}
