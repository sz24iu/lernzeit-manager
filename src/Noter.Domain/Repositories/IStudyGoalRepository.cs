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
        Task<StudyGoal?> GetByIdAsync(Guid id, Guid userId);
        Task<List<StudyGoalSummaryDto>> GetAllAsync(Guid userId);
        Task<List<StudyGoalSummaryDto>> GetCompletedAsync(Guid userId);

        Task AddAsync(CreateStudyGoalDto goal);

        Task UpdateAsync(StudyGoal goal);

        Task CompleteAndRemoveAsync(Guid id, Guid userId);
    }
}
