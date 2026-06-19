using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Dtos.StudyGoalDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Repositories
{
    public interface IStudyGoalRepository
    {
        Task<StudyGoal?> GetByIdAsync(Guid id);
        Task<List<StudyGoal>> GetAllAsync();

        Task AddAsync(CreateStudyGoalDto goal);

        Task UpdateAsync(StudyGoal goal);

        Task DeleteAsync(Guid id);
    }
}
