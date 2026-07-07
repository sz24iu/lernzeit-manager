using Noter.Domain.Entities.Enums;

namespace Noter.Domain.Entities.Dtos.MilestoneDto
{
    public class MilestoneWithStatsDto
    {
        public Guid Id { get; set; }
        public Guid StudyGoalId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public DateTime DueDateTime { get; set; }
        public GoalStatus Status { get; set; }
        public int SessionCount { get; set; }
        public int TotalTrackedMinutes { get; set; }
    }
}
