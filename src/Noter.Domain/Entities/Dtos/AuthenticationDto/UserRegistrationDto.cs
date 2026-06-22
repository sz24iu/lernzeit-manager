using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Entities.Dtos.AuthenticationDto
{
    public class UserRegistrationDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
