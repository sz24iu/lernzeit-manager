using Noter.Domain.Entities.Dtos.StudyGoalDto;
using Noter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Entities.DbEntities
{
    public class StudyGoal
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public string Title { get; private set; }
        public string Description { get; private set; }
        public GoalType Type { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public GoalStatus Status { get; private set; } = GoalStatus.Planned;

        public Guid UserId { get; private set; }

        public User User { get; private set; }
        public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();

        public StudyGoal(CreateStudyGoalDto dto)
        {
            Title = dto.Title;
            Description = dto.Description;
            Type = dto.Type;
            StartDate = dto.StartDate;
            EndDate = dto.EndDate;
            UserId = dto.UserId;
            Status = GoalStatus.Planned;
        }

        private StudyGoal() { } 
    }
}
