using System;
using System.Collections.Generic;

namespace CoreAxis.Modules.DynamicForm.Domain.ValueObjects
{
    public class SubmissionStats
    {
        public Guid FormId { get; set; }
        public int TotalSubmissions { get; set; }
        public int SubmissionsToday { get; set; }
        public int SubmissionsThisWeek { get; set; }
        public int SubmissionsThisMonth { get; set; }
        public DateTime? FirstSubmissionDate { get; set; }
        public DateTime? LastSubmissionDate { get; set; }
        public Dictionary<string, int> SubmissionsByStatus { get; set; } = new Dictionary<string, int>();
    }
}
