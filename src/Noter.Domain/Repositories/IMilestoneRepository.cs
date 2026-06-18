using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Dtos.MilestoneDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Repositories
{
    public interface IMilestoneRepository
    {
        Task<IQueryable<Milestone>> GetByStudyGoalIdAsync(Guid id);

        Task AddAsync(CreateMilestoneDto milestone);

        Task UpdateAsync(UpdateMilestoneStatusDto milestone);
    }
}
