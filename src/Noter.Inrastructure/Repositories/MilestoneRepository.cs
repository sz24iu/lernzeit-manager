using Microsoft.EntityFrameworkCore;
using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Dtos.MilestoneDto;
using Noter.Domain.Repositories;
using Noter.Inrastructure.Persistence.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Inrastructure.Repositories
{
    public class MilestoneRepository : IMilestoneRepository
    {
        private readonly NoterDbContext _context;

        private readonly IStudyGoalRepository _studyGoalRepository;
        public MilestoneRepository(NoterDbContext context, IStudyGoalRepository studyGoalRepository)
        {
            _context = context;
            _studyGoalRepository = studyGoalRepository;
        }
        public async Task AddAsync(CreateMilestoneDto milestone, Guid userId)
        {
            var studyGoal = await _studyGoalRepository.GetByIdAsync(milestone.StudyGoalId, userId);

            if (studyGoal == null)
                throw new Exception("Study goal not found");

            if (studyGoal.UserId != userId)
                throw new Exception("Forbidden");

            if (string.IsNullOrWhiteSpace(milestone.Title))
                throw new Exception("Milestone title is required");

            if (milestone.StartDateTime >= milestone.EndDateTime)
                throw new Exception("Milestone start time must be before end time");

            var newMilestone = new Milestone(milestone);

            await _context.AddAsync(newMilestone);
            await _context.SaveChangesAsync();
        }

        public async Task<List<MilestoneWithStatsDto>> GetByStudyGoalIdAsync(Guid id, Guid userId)
        {
            var studyGoal = await _studyGoalRepository.GetByIdAsync(id, userId);

            if (studyGoal == null)
            {
                return new List<MilestoneWithStatsDto>();
            }

            var milestones = await _context.Milestones
                .AsNoTracking()
                .Where(x => x.StudyGoalId == id)
                .OrderBy(x => x.StartDateTime)
                .ToListAsync();

            var milestoneIds = milestones.Select(x => x.Id).ToList();

            var sessionStats = new Dictionary<Guid, (int sessionCount, int totalMinutes)>();

            if (milestoneIds.Count > 0)
            {
                var grouped = await _context.StudySessions
                    .AsNoTracking()
                    .Where(x => x.UserId == userId && x.GoalId != null && milestoneIds.Contains(x.GoalId.Value))
                    .GroupBy(x => x.GoalId!.Value)
                    .Select(x => new
                    {
                        MilestoneId = x.Key,
                        SessionCount = x.Count(),
                        TotalTrackedMinutes = x.Sum(y => y.DurationMinutes)
                    })
                    .ToListAsync();

                sessionStats = grouped.ToDictionary(
                    x => x.MilestoneId,
                    x => (x.SessionCount, x.TotalTrackedMinutes));
            }

            return milestones.Select(x =>
            {
                var stats = sessionStats.TryGetValue(x.Id, out var value)
                    ? value
                    : (0, 0);

                return new MilestoneWithStatsDto
                {
                    Id = x.Id,
                    StudyGoalId = x.StudyGoalId,
                    Title = x.Title,
                    StartDateTime = x.StartDateTime,
                    EndDateTime = x.EndDateTime,
                    DueDateTime = x.DueDateTime,
                    Status = x.Status,
                    SessionCount = stats.Item1,
                    TotalTrackedMinutes = stats.Item2
                };
            }).ToList();
        }

        public async Task UpdateAsync(UpdateMilestoneStatusDto dto, Guid userId)
        {
            var milestone = await _context.Milestones
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (milestone == null)
                throw new Exception("Milestone not found");

            var studyGoal = await _studyGoalRepository.GetByIdAsync(milestone.StudyGoalId, userId);

            if (studyGoal == null)
                throw new Exception("Milestone not found");

            milestone.Status = dto.Status;

            _context.Milestones.Update(milestone);
            await _context.SaveChangesAsync();
        }

        public async Task AddTrackedSessionAsync(Guid milestoneId, int trackedMinutes, Guid userId)
        {
            if (trackedMinutes <= 0)
                throw new Exception("Tracked minutes must be greater than zero");

            var milestone = await _context.Milestones
                .FirstOrDefaultAsync(x => x.Id == milestoneId);

            if (milestone == null)
                throw new Exception("Milestone not found");

            var studyGoal = await _studyGoalRepository.GetByIdAsync(milestone.StudyGoalId, userId);

            if (studyGoal == null)
                throw new Exception("Milestone not found");

            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddMinutes(-trackedMinutes);

            var session = new StudySession(userId, milestoneId, startTime, endTime, trackedMinutes);

            await _context.StudySessions.AddAsync(session);
            await _context.SaveChangesAsync();
        }
    }
}
