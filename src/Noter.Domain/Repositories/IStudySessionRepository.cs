using Noter.Domain.Entities.DbEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Repositories
{
    public interface IStudySessionRepository
    {
        Task AddAsync(StudySession session);

        Task<List<StudySession>> GetUserSessionsAsync(Guid userId);
    }
}
