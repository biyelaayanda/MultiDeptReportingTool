using System.ComponentModel.DataAnnotations;

namespace MultiDeptReportingTool.DTOs
{
    public class CreateReportDto
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public string ReportType { get; set; } = string.Empty; // "Monthly", "Weekly", "Quarterly", "Annual", "Ad-hoc"
        
        [Required]
        public int DepartmentId { get; set; }
        
        [Required]
        public DateTime ReportPeriodStart { get; set; }
        
        [Required]
        public DateTime ReportPeriodEnd { get; set; }
        
        public string? Comments { get; set; }
        
        public List<CreateReportDataDto>? ReportData { get; set; }
    }

    public class UpdateReportDto
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public string ReportType { get; set; } = string.Empty;
        
        [Required]
        public DateTime ReportPeriodStart { get; set; }
        
        [Required]
        public DateTime ReportPeriodEnd { get; set; }
        
        public string? Comments { get; set; }
        
        public List<UpdateReportDataDto>? ReportData { get; set; }
    }

    public class ReportResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime ReportPeriodStart { get; set; }
        public DateTime ReportPeriodEnd { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedByUserId { get; set; }
        public string? ApprovedByUserName { get; set; }
        public string? Comments { get; set; }
        public List<ReportDataResponseDto> ReportData { get; set; } = new List<ReportDataResponseDto>();
    }

    public class CreateReportDataDto
    {
        [Required]
        public string FieldName { get; set; } = string.Empty;
        
        [Required]
        public string FieldType { get; set; } = string.Empty; // "Text", "Number", "Date", "Currency", "Boolean"
        
        public string? FieldValue { get; set; }
        public decimal? NumericValue { get; set; }
        public DateTime? DateValue { get; set; }
    }

    public class UpdateReportDataDto
    {
        public int? Id { get; set; } // Null for new items
        
        [Required]
        public string FieldName { get; set; } = string.Empty;
        
        [Required]
        public string FieldType { get; set; } = string.Empty;
        
        public string? FieldValue { get; set; }
        public decimal? NumericValue { get; set; }
        public DateTime? DateValue { get; set; }
    }

    public class ReportDataResponseDto
    {
        public int Id { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string? FieldValue { get; set; }
        public decimal? NumericValue { get; set; }
        public DateTime? DateValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ReportSubmissionDto
    {
        public string? Comments { get; set; }
    }

    public class ReportApprovalDto
    {
        [Required]
        public bool Approved { get; set; }
        
        public string? Comments { get; set; }
    }

    public class ReportFilterDto
    {
        public int? DepartmentId { get; set; }
        public string? Status { get; set; }
        public string? ReportType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CreatedByUserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";
    }
}
