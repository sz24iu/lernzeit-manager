using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Noter.Domain.Entities.ConfigEntities;
using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Dtos.AuthenticationDto;
using Noter.Inrastructure.Persistence.DbContexts;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Inrastructure.Repositories
{
    internal class TokenRepository
    {
        public static async Task<TokenDataDto> GenerateJwtToken(User user, JwtConfig jwtConfig, NoterDbContext context)
        {
            var jwtHandler = new JwtSecurityTokenHandler();

            IdentityUser tokenUser = new IdentityUser()
            {
                Id = user.Id.ToString(),
                Email = user.Email
            };

            var key = Encoding.ASCII.GetBytes(jwtConfig.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim("Id", tokenUser.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
                Expires = DateTime.UtcNow.Add(jwtConfig.ExpiryTimeFrame),
                SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = jwtHandler.CreateToken(tokenDescriptor);

            var jwtToken = jwtHandler.WriteToken(token);

            var refreshToken = new RefreshToken()
            {
                Token = $"{RandomStringGenerator(25)}_{Guid.NewGuid()}",
                UserId = user.Id,
                IsRevoked = false,
                IsUsed = false,
                JwtId = token.Id,
                ExpiryDate = DateTime.UtcNow.AddMonths(6)
            };

            context.RefreshTokens.Add(refreshToken);
            context.SaveChanges();

            var tokenData = new TokenDataDto
            {
                JwtToken = jwtToken,
                RefreshToken = refreshToken.Token
            };

            return tokenData;
        }

        public static Task<string> VerifyToken(TokenDto tokenDto, TokenValidationParameters tokenValidationParameters, NoterDbContext context)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(tokenDto.Token, tokenValidationParameters, out var validatedToken);

            string errorMessage = string.Empty;

            if (validatedToken is JwtSecurityToken jwtSecurityToken)
            {
                var jwtAlg = jwtSecurityToken
                    .Header
                    .Alg
                    .Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase);

                if (!jwtAlg)
                    return null;

                var utcExpiryDate =
                    long.Parse(principal
                        .Claims
                        .FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp)
                        .Value);

                var expiryDate = UnixTimeStampToDateTime(utcExpiryDate);

                if (expiryDate > DateTime.UtcNow)
                    errorMessage = "Jwt Token Has Not Expired";
            }

            var refreshToken = context.RefreshTokens.FirstOrDefault(x => x.Token == tokenDto.RefreshToken);

            if (refreshToken is null)
                errorMessage = "Invalid Refresh Token";

            if (refreshToken.ExpiryDate < DateTime.UtcNow)
                errorMessage = "Refresh Token Has Expired. Login Again";

            if (refreshToken.IsUsed)
                errorMessage = "Refresh Token Has Been Used And Cannot Be Reused";

            if (refreshToken.IsRevoked)
                errorMessage = "Refresh Token Has Been Revoked And Cannot Be Used";

            var jti = principal.Claims.SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

            if (refreshToken.JwtId != jti)
                errorMessage = "Refresh Token Reference Does Not Match The Jwt Token";

            refreshToken.IsUsed = true;

            context.RefreshTokens.Update(refreshToken);

            context.SaveChanges();

            return Task.FromResult(errorMessage);
        }

        private static string RandomStringGenerator(int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            return new string(
                Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
        }

        private static DateTime UnixTimeStampToDateTime(long unixDate)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, 0, DateTimeKind.Utc);

            dateTime.AddSeconds(unixDate).ToUniversalTime();

            return dateTime;
        }
    }
}
