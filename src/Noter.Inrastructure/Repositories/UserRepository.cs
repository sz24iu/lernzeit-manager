using Microsoft.EntityFrameworkCore;
using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Dtos.StudyGoalDto;
using Noter.Domain.Entities.Dtos.UserDto;
using Noter.Domain.Repositories;
using Noter.Inrastructure.Persistence.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Inrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly NoterDbContext _context;

        public UserRepository(NoterDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(CreateUserDto userDto)
        {
            var user = await _context.Users
                .Where(x => x.Email == userDto.Email)
                .FirstOrDefaultAsync();

            if (user != null)
                throw new ArgumentException("User with this email is already exist!");

            var newUser = new User(userDto);

            await _context.AddAsync(newUser);
            await _context.SaveChangesAsync();
        }

        public async Task<User> GetByIdAsync(Guid id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
                throw new Exception("User not found");

            return user;
        }
    }
}
