using Noter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Entities.Dtos.MilestoneDto
{
    public class UpdateMilestoneStatusDto
    {
        public Guid Id { get; set; }
        public GoalStatus Status { get; set; }
    }
}
