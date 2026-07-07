using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Dtos.MilestoneDto;
using Noter.Domain.Entities.Dtos.StudySessionPlan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Repositories
{
    public interface IStudySessionPlanRepository
    {
        Task<IQueryable<StudySessionPlan>> GetByStudyGoalIdAsync(Guid id);

        Task AddAsync(CreateStudySessionPlanDto studyPlan);
    }
}
