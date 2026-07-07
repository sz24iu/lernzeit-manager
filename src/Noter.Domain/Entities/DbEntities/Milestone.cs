using Noter.Domain.Entities.Dtos.MilestoneDto;
using Noter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Entities.DbEntities
{
    public class Milestone
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid StudyGoalId { get; set; }

        public string Title { get; set; }

        public DateTime DueDateTime { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public GoalStatus Status { get; set; }

        public ICollection<StudySessionPlan> StudySessionPlans { get; set; }

        public Milestone(CreateMilestoneDto dto)
        {
            Title = dto.Title;
            StudyGoalId = dto.StudyGoalId;
            StartDateTime = dto.StartDateTime;
            EndDateTime = dto.EndDateTime;
            DueDateTime = dto.EndDateTime;
            Status = GoalStatus.Planned;
        }

        public Milestone()
        {
        }

    }
}
