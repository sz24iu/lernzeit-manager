using Noter.Domain.Entities.Dtos.StudyGoalDto;
using Noter.Domain.Entities.Dtos.UserDto;
using Noter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Entities.DbEntities
{
    public class User
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Email { get; private set; }

        public ICollection<StudyGoal> Goals { get; private set; }

        public User(CreateUserDto dto)
        {
            Email = dto.Email;
        }

        private User() { }
    }
}
