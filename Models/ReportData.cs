using System;

namespace MultiDeptReportingTool.Models
{
    public class ReportData
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty; // "Text", "Number", "Date", "Currency", etc.
        public string? FieldValue { get; set; }
        public decimal? NumericValue { get; set; }
        public DateTime? DateValue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual Report Report { get; set; } = null!;
    }
}
