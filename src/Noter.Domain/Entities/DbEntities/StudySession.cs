using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Entities.DbEntities
{
    public class StudySession
    {
        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public Guid? GoalId { get; private set; }

        public DateTime StartTime { get; private set; }

        public DateTime EndTime { get; private set; }

        public int DurationMinutes { get; private set; }
    }
}
