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
        Task<List<Milestone>> GetByStudyGoalIdAsync(Guid id, Guid userId);

        Task AddAsync(CreateMilestoneDto milestone, Guid userId);

        Task UpdateAsync(UpdateMilestoneStatusDto milestone, Guid userId);
    }
}
