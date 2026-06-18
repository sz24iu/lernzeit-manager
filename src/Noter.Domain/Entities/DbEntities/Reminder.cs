using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Entities.DbEntities
{
    public class Reminder
    {
        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public string Message { get; private set; }

        public DateTime ReminderTime { get; private set; }

        public bool IsSent { get; private set; }
    }
}
