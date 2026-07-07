using Microsoft.EntityFrameworkCore;
using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Dtos.StudyGoalDto;
using Noter.Domain.Entities.Enums;
using Noter.Domain.Repositories;
using Noter.Inrastructure.Persistence.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Inrastructure.Repositories
{
    public class StudyGoalRepository : IStudyGoalRepository
    {
        private readonly NoterDbContext _context;

        public StudyGoalRepository(NoterDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(CreateStudyGoalDto studyGoalDto)
        {
            if (string.IsNullOrWhiteSpace(studyGoalDto.Title))
                throw new Exception("Study goal title is required");

            var entity = new StudyGoal(studyGoalDto);

            await _context.StudyGoals.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task CompleteAndRemoveAsync(Guid id, Guid userId)
        {
            var goal = await _context.StudyGoals
                .Include(x => x.Milestones)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (goal == null)
                throw new Exception("Study goal not found");

            if (goal.Milestones.Any(x => x.Status != GoalStatus.Completed))
                throw new InvalidOperationException("Alle Meilensteine muessen zuerst auf 'Abgeschlossen' gesetzt werden.");

            goal.MarkAsCompleted();
            await _context.SaveChangesAsync();
        }

        public async Task<List<StudyGoalSummaryDto>> GetAllAsync(Guid userId)
        {
            var goals = await _context.StudyGoals
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.Status != GoalStatus.Completed)
                .ToListAsync();

            return await MapStudyGoalSummariesAsync(goals, userId);
        }

        public async Task<List<StudyGoalSummaryDto>> GetCompletedAsync(Guid userId)
        {
            var goals = await _context.StudyGoals
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.Status == GoalStatus.Completed)
                .OrderByDescending(x => x.EndDate)
                .ToListAsync();

            return await MapStudyGoalSummariesAsync(goals, userId);
        }

        private async Task<List<StudyGoalSummaryDto>> MapStudyGoalSummariesAsync(List<StudyGoal> goals, Guid userId)
        {
            if (goals.Count == 0)
            {
                return new List<StudyGoalSummaryDto>();
            }

            var goalIds = goals.Select(x => x.Id).ToList();

            var totalsByGoal = new Dictionary<Guid, int>();
            var milestoneTotalsByGoal = new Dictionary<Guid, (int total, int completed)>();

            if (goalIds.Count > 0)
            {
                var totals = await _context.Milestones
                    .AsNoTracking()
                    .Where(x => goalIds.Contains(x.StudyGoalId))
                    .GroupJoin(
                        _context.StudySessions.AsNoTracking().Where(x => x.UserId == userId && x.GoalId != null),
                        milestone => milestone.Id,
                        session => session.GoalId!.Value,
                        (milestone, sessions) => new
                        {
                            milestone.StudyGoalId,
                            Minutes = sessions.Sum(s => s.DurationMinutes)
                        })
                    .GroupBy(x => x.StudyGoalId)
                    .Select(x => new
                    {
                        StudyGoalId = x.Key,
                        TotalTrackedMinutes = x.Sum(y => y.Minutes)
                    })
                    .ToListAsync();

                totalsByGoal = totals.ToDictionary(x => x.StudyGoalId, x => x.TotalTrackedMinutes);

                var milestoneTotals = await _context.Milestones
                    .AsNoTracking()
                    .Where(x => goalIds.Contains(x.StudyGoalId))
                    .GroupBy(x => x.StudyGoalId)
                    .Select(x => new
                    {
                        StudyGoalId = x.Key,
                        TotalMilestones = x.Count(),
                        CompletedMilestones = x.Count(y => y.Status == GoalStatus.Completed)
                    })
                    .ToListAsync();

                milestoneTotalsByGoal = milestoneTotals.ToDictionary(
                    x => x.StudyGoalId,
                    x => (x.TotalMilestones, x.CompletedMilestones));
            }

            return goals.Select(x => new StudyGoalSummaryDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Type = x.Type,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status,
                UserId = x.UserId,
                TotalTrackedMinutes = totalsByGoal.TryGetValue(x.Id, out var total) ? total : 0,
                TotalMilestones = milestoneTotalsByGoal.TryGetValue(x.Id, out var milestones) ? milestones.total : 0,
                CompletedMilestones = milestoneTotalsByGoal.TryGetValue(x.Id, out var milestoneSummary) ? milestoneSummary.completed : 0
            }).ToList();
        }

        public async Task<StudyGoal?> GetByIdAsync(Guid id, Guid userId)
        {
            return await _context.StudyGoals.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        }

        public Task UpdateAsync(StudyGoal goal)
        {
            throw new NotImplementedException();
        }
    }
}
