using Noter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Entities.DbEntities
{
    public class LearningTimer
    {
        public Guid Id { get; private set; }

        public DateTime StartedAt { get; private set; }

        public DateTime? StoppedAt { get; private set; }

        public TimerState State { get; private set; }
    }
}
