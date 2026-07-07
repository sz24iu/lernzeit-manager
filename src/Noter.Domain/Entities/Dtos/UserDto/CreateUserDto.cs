using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Entities.Dtos.UserDto
{
    public class CreateUserDto
    {
        public string Email { get; set; }
        public string HashPassword { get; set; }
    }
}
