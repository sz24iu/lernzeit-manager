using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Entities.ConfigEntities
{
    public class TokenDataDto
    {
        public string JwtToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
