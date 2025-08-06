namespace MultiDeptReportingTool.Models
{
    public class Report
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ReportType { get; set; } = string.Empty; // "Monthly", "Weekly", "Quarterly", etc.
        public string Status { get; set; } = "Draft"; // Draft, Pending, Approved, Published
        public int DepartmentId { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime ReportPeriodStart { get; set; }
        public DateTime ReportPeriodEnd { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedByUserId { get; set; }
        public string? Comments { get; set; }

        // Navigation properties
        public virtual Department Department { get; set; } = null!;
        public virtual Users CreatedByUser { get; set; } = null!;
        public virtual Users? ApprovedByUser { get; set; }
        public virtual ICollection<ReportData> ReportData { get; set; } = new List<ReportData>();
    }
}
