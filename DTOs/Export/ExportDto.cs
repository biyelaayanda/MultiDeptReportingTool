using System.ComponentModel.DataAnnotations;

namespace MultiDeptReportingTool.DTOs.Export
{
    /// <summary>
    /// Export request DTO for generating reports in various formats
    /// </summary>
    public class ExportRequestDto
    {
        [Required(ErrorMessage = "Report type is required")]
        public string ReportType { get; set; } = string.Empty; // dashboard, departmental, custom
        
        [Required(ErrorMessage = "Export format is required")]
        public string Format { get; set; } = string.Empty; // pdf, excel, csv, json
        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<string> Departments { get; set; } = new List<string>();
        public List<string> IncludeFields { get; set; } = new List<string>();
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeSummary { get; set; } = true;
        public string FileName { get; set; } = string.Empty;
        public Dictionary<string, object> CustomFilters { get; set; } = new Dictionary<string, object>();
        
        // User context fields (set by backend)
        public int? UserId { get; set; }
        public string? UserRole { get; set; }
        public string? UserDepartment { get; set; }
        
        // Report-specific fields
        public List<int>? ReportIds { get; set; } = new List<int>();
    }

    /// <summary>
    /// Export response DTO
    /// </summary>
    public class ExportResponseDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] FileData { get; set; } = Array.Empty<byte>();
        public long FileSize { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string DownloadUrl { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Email notification DTO
    /// </summary>
    public class EmailNotificationDto
    {
        [Required(ErrorMessage = "Recipient email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string ToEmail { get; set; } = string.Empty;
        
        public List<string> CcEmails { get; set; } = new List<string>();
        
        [Required(ErrorMessage = "Subject is required")]
        public string Subject { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Message body is required")]
        public string Body { get; set; } = string.Empty;
        
        public bool IsHtml { get; set; } = true;
        public List<EmailAttachmentDto> Attachments { get; set; } = new List<EmailAttachmentDto>();
        public string Priority { get; set; } = "Normal"; // High, Normal, Low
        public bool SendImmediately { get; set; } = true;
        public DateTime? ScheduledTime { get; set; }
    }

    public class EmailAttachmentDto
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] FileData { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Scheduled report DTO
    /// </summary>
    public class ScheduledReportDto
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Report name is required")]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Report type is required")]
        public string ReportType { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Schedule frequency is required")]
        public string Frequency { get; set; } = string.Empty; // daily, weekly, monthly, quarterly
        
        public List<string> Recipients { get; set; } = new List<string>();
        public ExportRequestDto ExportSettings { get; set; } = new ExportRequestDto();
        public bool IsActive { get; set; } = true;
        public DateTime NextRun { get; set; }
        public DateTime LastRun { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Chart configuration DTO for export
    /// </summary>
    public class ChartConfigDto
    {
        public string ChartType { get; set; } = "line"; // line, bar, pie, area, scatter
        public string Title { get; set; } = string.Empty;
        public string XAxisLabel { get; set; } = string.Empty;
        public string YAxisLabel { get; set; } = string.Empty;
        public List<ChartDataSeriesDto> DataSeries { get; set; } = new List<ChartDataSeriesDto>();
        public int Width { get; set; } = 800;
        public int Height { get; set; } = 400;
        public bool ShowLegend { get; set; } = true;
        public bool ShowGrid { get; set; } = true;
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class ChartDataSeriesDto
    {
        public string Name { get; set; } = string.Empty;
        public List<ChartDataPointDto> Data { get; set; } = new List<ChartDataPointDto>();
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class ChartDataPointDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime? Date { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}
