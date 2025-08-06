using System.ComponentModel.DataAnnotations;

namespace MultiDeptReportingTool.DTOs.DepartmentSpecific
{
    /// <summary>
    /// HR Department specific report DTO for workforce trends and headcount metrics
    /// </summary>
    public class HRReportDto
    {
        [Required]
        public int TotalEmployees { get; set; }
        
        [Required]
        public int NewHires { get; set; }
        
        [Required]
        public int Terminations { get; set; }
        
        public int NetHeadcountChange => NewHires - Terminations;
        
        [Range(0, 100)]
        public decimal TurnoverRate { get; set; }
        
        [Range(0, 10)]
        public decimal EmployeeSatisfactionScore { get; set; }
        
        public List<DepartmentHeadcountDto> DepartmentBreakdown { get; set; } = new List<DepartmentHeadcountDto>();
        public List<PerformanceMetricDto> PerformanceMetrics { get; set; } = new List<PerformanceMetricDto>();
        
        public string? TrainingProgramsCompleted { get; set; }
        
        [Range(0, 5)]
        public decimal AveragePerformanceRating { get; set; }
        
        public int OpenPositions { get; set; }
        public decimal RecruitmentCost { get; set; }
        public int TrainingHours { get; set; }
        public string? HRInitiatives { get; set; }
        public string? WorkforceRecommendations { get; set; }
    }

    public class DepartmentHeadcountDto
    {
        [Required]
        public string DepartmentName { get; set; } = string.Empty;
        
        [Required]
        public int CurrentCount { get; set; }
        
        [Required]
        public int TargetCount { get; set; }
        
        public int Variance => CurrentCount - TargetCount;
        public decimal VariancePercentage => TargetCount > 0 ? ((decimal)Variance / TargetCount) * 100 : 0;
        
        public int NewHires { get; set; }
        public int Terminations { get; set; }
    }

    public class PerformanceMetricDto
    {
        [Required]
        public string MetricName { get; set; } = string.Empty;
        
        [Required]
        public decimal CurrentValue { get; set; }
        
        [Required]
        public decimal TargetValue { get; set; }
        
        public string? Unit { get; set; }
        public decimal VariancePercentage => TargetValue > 0 ? ((CurrentValue - TargetValue) / TargetValue) * 100 : 0;
        public string? Comments { get; set; }
    }

    public class CreateHRReportDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string ReportType { get; set; } = "HR";
        
        [Required]
        public HRReportDto HRData { get; set; } = new HRReportDto();
        
        public string? Comments { get; set; }
    }

    public class HRReportResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public HRReportDto HRData { get; set; } = new HRReportDto();
        public string? Comments { get; set; }
    }
}
