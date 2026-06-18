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
        public async Task AddAsync(CreateMilestoneDto milestone)
        {
            var studyGoal = await _studyGoalRepository.GetByIdAsync(milestone.StudyGoalId);

            if (studyGoal == null)
                throw new Exception("Study goal not found");

            var newMilestone = new Milestone(milestone);

            await _context.AddAsync(newMilestone);
            await _context.SaveChangesAsync();
        }

        public async Task<IQueryable<Milestone>> GetByStudyGoalIdAsync(Guid id)
        {
            var result = _context.Milestones
                .Where(x => x.StudyGoalId == id);

            return result;
        }

        public async Task UpdateAsync(UpdateMilestoneStatusDto dto)
        {
            var milestone = await _context.Milestones
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (milestone == null)
                throw new Exception("Milestone not found");

            milestone.Status = dto.Status;

            _context.Milestones.Update(milestone);
            await _context.SaveChangesAsync();
        }
    }
}
