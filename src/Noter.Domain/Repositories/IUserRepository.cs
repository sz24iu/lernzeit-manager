using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Dtos.StudyGoalDto;
using Noter.Domain.Entities.Dtos.UserDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(Guid id);

        Task AddAsync(CreateUserDto goal);
    }
}
