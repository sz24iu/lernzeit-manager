using Noter.Domain.Entities.Enums;

namespace Noter.Domain.Entities.Dtos.StudyGoalDto
{
    public class StudyGoalSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public GoalType Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public GoalStatus Status { get; set; }
        public Guid UserId { get; set; }
        public int TotalTrackedMinutes { get; set; }
        public int TotalMilestones { get; set; }
        public int CompletedMilestones { get; set; }
    }
}
