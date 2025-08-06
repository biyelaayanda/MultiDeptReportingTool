using System.ComponentModel.DataAnnotations;

namespace MultiDeptReportingTool.DTOs.DepartmentSpecific
{
    /// <summary>
    /// Operations Department specific report DTO for project status and KPIs
    /// </summary>
    public class OperationsReportDto
    {
        [Required]
        public int TotalProjects { get; set; }
        
        [Required]
        public int CompletedProjects { get; set; }
        
        [Required]
        public int OngoingProjects { get; set; }
        
        [Required]
        public int DelayedProjects { get; set; }
        
        public decimal ProjectCompletionRate => TotalProjects > 0 ? ((decimal)CompletedProjects / TotalProjects) * 100 : 0;
        
        [Range(0, 100)]
        public decimal OverallEfficiencyScore { get; set; }
        
        public List<ProjectStatusDto> ProjectStatuses { get; set; } = new List<ProjectStatusDto>();
        public List<ResourceUtilizationDto> ResourceUtilization { get; set; } = new List<ResourceUtilizationDto>();
        public List<OperationalKPIDto> OperationalKPIs { get; set; } = new List<OperationalKPIDto>();
        
        public string? IssuesAndChallenges { get; set; }
        public decimal TotalBudgetAllocated { get; set; }
        public decimal TotalBudgetUtilized { get; set; }
        public decimal BudgetUtilizationRate => TotalBudgetAllocated > 0 ? (TotalBudgetUtilized / TotalBudgetAllocated) * 100 : 0;
        public string? OperationalHighlights { get; set; }
        public string? RecommendedActions { get; set; }
    }

    public class ProjectStatusDto
    {
        [Required]
        public string ProjectName { get; set; } = string.Empty;
        
        [Required]
        public string Status { get; set; } = string.Empty; // "On Track", "Delayed", "Completed", "At Risk", "On Hold"
        
        [Range(0, 100)]
        public decimal CompletionPercentage { get; set; }
        
        public DateTime? EstimatedCompletionDate { get; set; }
        public DateTime? ActualStartDate { get; set; }
        public string? ProjectManager { get; set; }
        
        [Range(0, 100)]
        public decimal? BudgetUtilization { get; set; }
        
        public string? KeyMilestones { get; set; }
        public string? RisksAndIssues { get; set; }
        public string Priority { get; set; } = "Medium"; // "High", "Medium", "Low"
    }

    public class ResourceUtilizationDto
    {
        [Required]
        public string ResourceType { get; set; } = string.Empty;
        
        [Range(0, 100)]
        public decimal UtilizationPercentage { get; set; }
        
        public int TotalCapacity { get; set; }
        public int CurrentAllocation { get; set; }
        public int AvailableCapacity => TotalCapacity - CurrentAllocation;
        public string? Comments { get; set; }
        public decimal? CostPerUnit { get; set; }
    }

    public class OperationalKPIDto
    {
        [Required]
        public string KPIName { get; set; } = string.Empty;
        
        [Required]
        public decimal CurrentValue { get; set; }
        
        [Required]
        public decimal TargetValue { get; set; }
        
        public string? Unit { get; set; }
        public string? Trend { get; set; } = "Stable"; // "Improving", "Declining", "Stable"
        public decimal VariancePercentage => TargetValue > 0 ? ((CurrentValue - TargetValue) / TargetValue) * 100 : 0;
        public string? Comments { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public class CreateOperationsReportDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string ReportType { get; set; } = "Operations";
        
        [Required]
        public OperationsReportDto OperationsData { get; set; } = new OperationsReportDto();
        
        public string? Comments { get; set; }
    }

    public class OperationsReportResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public OperationsReportDto OperationsData { get; set; } = new OperationsReportDto();
        public string? Comments { get; set; }
    }
}
