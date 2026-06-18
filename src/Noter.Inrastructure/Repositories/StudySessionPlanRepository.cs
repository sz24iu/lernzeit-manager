using Microsoft.EntityFrameworkCore;
using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Dtos.StudySessionPlan;
using Noter.Domain.Repositories;
using Noter.Inrastructure.Persistence.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Inrastructure.Repositories
{
    public class StudySessionPlanRepository : IStudySessionPlanRepository
    {
        private readonly NoterDbContext _context;

        public StudySessionPlanRepository(NoterDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(CreateStudySessionPlanDto studyPlanDto)
        {
            var milestone = await _context.Milestones
                .Where(x => x.Id == studyPlanDto.MilestoneId).FirstOrDefaultAsync();

            if(milestone == null)
                throw new Exception("Milestone not found");

            var studySessionPlan = new StudySessionPlan(studyPlanDto);

            await _context.StudySessionPlans.AddAsync(studySessionPlan);
            await _context.SaveChangesAsync();
        }

        public async Task<IQueryable<StudySessionPlan>> GetByStudyGoalIdAsync(Guid id)
        {
            return _context.StudySessionPlans.Where(x => x.MilestoneId == id);
        }
    }
}
