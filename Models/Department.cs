using System;
using System.Collections.Generic;

namespace MultiDeptReportingTool.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Users> Users { get; set; } = new List<Users>();
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
