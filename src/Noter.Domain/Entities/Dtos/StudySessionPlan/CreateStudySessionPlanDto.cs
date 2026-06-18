using Noter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Entities.Dtos.StudySessionPlan
{
    public class CreateStudySessionPlanDto
    {
        public DateTime PlannedStart { get; set; }

        public int PlannedMinutes { get; set; }

        public Guid MilestoneId { get; set; }

    }
}
