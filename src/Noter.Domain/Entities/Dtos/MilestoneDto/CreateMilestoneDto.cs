using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Domain.Entities.Dtos.MilestoneDto
{
    public class CreateMilestoneDto
    {

        public Guid StudyGoalId { get; set; }

        public string Title { get; set; }

        public DateTime DueDateTime { get; set; }
    }
}
