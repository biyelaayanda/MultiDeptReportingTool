using System.ComponentModel.DataAnnotations;

namespace MultiDeptReportingTool.DTOs.DepartmentSpecific
{
    /// <summary>
    /// IT Department specific report DTO for system performance and ticketing metrics
    /// </summary>
    public class ITReportDto
    {
        [Range(0, 100)]
        public decimal SystemUptimePercentage { get; set; }
        
        [Required]
        public int TotalTickets { get; set; }
        
        [Required]
        public int ResolvedTickets { get; set; }
        
        [Required]
        public int OpenTickets { get; set; }
        
        public decimal TicketResolutionRate => TotalTickets > 0 ? ((decimal)ResolvedTickets / TotalTickets) * 100 : 0;
        
        public decimal AverageResolutionTime { get; set; } // in hours
        
        [Range(0, 10)]
        public decimal CustomerSatisfactionScore { get; set; }
        
        public List<SystemPerformanceDto> SystemMetrics { get; set; } = new List<SystemPerformanceDto>();
        public List<TicketCategoryDto> TicketBreakdown { get; set; } = new List<TicketCategoryDto>();
        public List<ITAssetDto> AssetInventory { get; set; } = new List<ITAssetDto>();
        public List<SecurityIncidentDto> SecurityIncidents { get; set; } = new List<SecurityIncidentDto>();
        
        public int TotalServers { get; set; }
        public int ServersOnline { get; set; }
        public decimal ServerUptimePercentage => TotalServers > 0 ? ((decimal)ServersOnline / TotalServers) * 100 : 0;
        
        public decimal NetworkUtilization { get; set; } // percentage
        public int BackupSuccessRate { get; set; } // percentage
        public string? ITInitiatives { get; set; }
        public string? TechnicalRecommendations { get; set; }
        public decimal InfrastructureCosts { get; set; }
    }

    public class SystemPerformanceDto
    {
        [Required]
        public string SystemName { get; set; } = string.Empty;
        
        [Range(0, 100)]
        public decimal UptimePercentage { get; set; }
        
        [Range(0, 100)]
        public decimal CPUUtilization { get; set; }
        
        [Range(0, 100)]
        public decimal MemoryUtilization { get; set; }
        
        [Range(0, 100)]
        public decimal DiskUtilization { get; set; }
        
        public decimal ResponseTime { get; set; } // in milliseconds
        public int ConcurrentUsers { get; set; }
        public string? PerformanceNotes { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public string SystemType { get; set; } = string.Empty; // "Server", "Application", "Database", "Network"
    }

    public class TicketCategoryDto
    {
        [Required]
        public string Category { get; set; } = string.Empty;
        
        [Required]
        public int TicketCount { get; set; }
        
        [Required]
        public int ResolvedCount { get; set; }
        
        public decimal ResolutionRate => TicketCount > 0 ? ((decimal)ResolvedCount / TicketCount) * 100 : 0;
        
        public decimal AverageResolutionTime { get; set; } // in hours
        
        public string Priority { get; set; } = "Medium"; // "Critical", "High", "Medium", "Low"
        public string? CommonIssues { get; set; }
        public string? ResolutionTrends { get; set; }
    }

    public class ITAssetDto
    {
        [Required]
        public string AssetName { get; set; } = string.Empty;
        
        [Required]
        public string AssetType { get; set; } = string.Empty; // "Hardware", "Software", "License"
        
        public string? Location { get; set; }
        public string Status { get; set; } = "Active"; // "Active", "Maintenance", "Retired", "Disposed"
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiration { get; set; }
        public decimal? AssetValue { get; set; }
        public string? AssignedTo { get; set; }
        public string? Vendor { get; set; }
        public string? SerialNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class SecurityIncidentDto
    {
        [Required]
        public string IncidentType { get; set; } = string.Empty;
        
        [Required]
        public string Severity { get; set; } = string.Empty; // "Critical", "High", "Medium", "Low"
        
        [Required]
        public DateTime DetectedDate { get; set; }
        
        public DateTime? ResolvedDate { get; set; }
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public string? Response { get; set; }
        public string Status { get; set; } = "Open"; // "Open", "Investigating", "Contained", "Resolved"
        public string? AffectedSystems { get; set; }
        public string? ImpactAssessment { get; set; }
        public string? PreventiveMeasures { get; set; }
        public bool DataCompromised { get; set; } = false;
        public string? ReportedBy { get; set; }
    }

    public class CreateITReportDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string ReportType { get; set; } = "IT";
        
        [Required]
        public ITReportDto ITData { get; set; } = new ITReportDto();
        
        public string? Comments { get; set; }
    }

    public class ITReportResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public ITReportDto ITData { get; set; } = new ITReportDto();
        public string? Comments { get; set; }
    }
}
