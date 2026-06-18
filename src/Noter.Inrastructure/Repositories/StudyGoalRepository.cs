using Microsoft.EntityFrameworkCore;
using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Dtos.StudyGoalDto;
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
            var entity = new StudyGoal(studyGoalDto);

            await _context.StudyGoals.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public Task DeleteAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<IQueryable<StudyGoal>> GetAllsync()
        {
            var result = _context.StudyGoals;
            return result;
        }

        public async Task<StudyGoal?> GetByIdAsync(Guid id)
        {
            return await _context.StudyGoals.FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task UpdateAsync(StudyGoal goal)
        {
            throw new NotImplementedException();
        }
    }
}
