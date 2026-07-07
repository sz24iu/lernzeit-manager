using Noter.Domain.Entities.Dtos.StudySessionPlan;
using Noter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Entities.DbEntities
{
    public class StudySessionPlan
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public DateTime PlannedStart { get; private set; }

        public int PlannedMinutes { get; private set; }

        public SessionStatus Status { get; private set; }

        public Guid MilestoneId {  get; private set; }

        public StudySessionPlan(CreateStudySessionPlanDto dto)
        {
            MilestoneId = dto.MilestoneId;
            PlannedMinutes = dto.PlannedMinutes;
            PlannedStart = dto.PlannedStart;
        }

        public StudySessionPlan()
        { }
    }
}
