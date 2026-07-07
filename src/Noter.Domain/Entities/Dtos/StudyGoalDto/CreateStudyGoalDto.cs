using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Entities.Dtos.StudyGoalDto
{
    public class CreateStudyGoalDto
    {

        public string Title { get; set; }

        public string Description { get; set; }

        public GoalType Type { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public Guid UserId { get; set; }
    }
}
