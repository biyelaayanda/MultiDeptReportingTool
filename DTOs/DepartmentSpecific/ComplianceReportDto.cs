using System.ComponentModel.DataAnnotations;

namespace MultiDeptReportingTool.DTOs.DepartmentSpecific
{
    /// <summary>
    /// Compliance Department specific report DTO for risk assessment and incident reporting
    /// </summary>
    public class ComplianceReportDto
    {
        [Required]
        public int TotalRiskAssessments { get; set; }
        
        [Required]
        public int HighRiskIssues { get; set; }
        
        [Required]
        public int MediumRiskIssues { get; set; }
        
        [Required]
        public int LowRiskIssues { get; set; }
        
        [Required]
        public int IncidentsReported { get; set; }
        
        [Required]
        public int IncidentsResolved { get; set; }
        
        public decimal IncidentResolutionRate => IncidentsReported > 0 ? ((decimal)IncidentsResolved / IncidentsReported) * 100 : 0;
        
        public List<RiskAssessmentDto> RiskAssessments { get; set; } = new List<RiskAssessmentDto>();
        public List<ComplianceIncidentDto> Incidents { get; set; } = new List<ComplianceIncidentDto>();
        public List<RegulatoryCheckDto> RegulatoryChecklist { get; set; } = new List<RegulatoryCheckDto>();
        
        public string? ComplianceStatus { get; set; } = "Compliant";
        
        [Range(0, 100)]
        public decimal OverallComplianceScore { get; set; }
        
        public DateTime? LastAuditDate { get; set; }
        public DateTime? NextAuditDate { get; set; }
        public string? AuditFindings { get; set; }
        public string? RecommendedActions { get; set; }
        public int PendingActions { get; set; }
        public int CompletedActions { get; set; }
    }

    public class RiskAssessmentDto
    {
        [Required]
        public string RiskCategory { get; set; } = string.Empty;
        
        [Required]
        public string RiskLevel { get; set; } = string.Empty; // "High", "Medium", "Low"
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public string? MitigationPlan { get; set; }
        public DateTime? AssessmentDate { get; set; }
        public string? ResponsiblePerson { get; set; }
        
        [Range(1, 5)]
        public int ImpactScore { get; set; } = 3;
        
        [Range(1, 5)]
        public int LikelihoodScore { get; set; } = 3;
        
        public int RiskScore => ImpactScore * LikelihoodScore;
        public string? ControlMeasures { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string Status { get; set; } = "Active"; // "Active", "Mitigated", "Closed"
    }

    public class ComplianceIncidentDto
    {
        [Required]
        public string IncidentType { get; set; } = string.Empty;
        
        [Required]
        public string Severity { get; set; } = string.Empty; // "Critical", "High", "Medium", "Low"
        
        [Required]
        public DateTime ReportedDate { get; set; }
        
        public DateTime? ResolvedDate { get; set; }
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public string? ResolutionAction { get; set; }
        public string? Status { get; set; } = "Open"; // "Open", "In Progress", "Resolved", "Closed"
        public string? ReportedBy { get; set; }
        public string? AssignedTo { get; set; }
        public string? RootCause { get; set; }
        public string? PreventiveMeasures { get; set; }
        public decimal? FinancialImpact { get; set; }
    }

    public class RegulatoryCheckDto
    {
        [Required]
        public string RegulationName { get; set; } = string.Empty;
        
        [Required]
        public bool IsCompliant { get; set; }
        
        public DateTime? LastReviewDate { get; set; }
        public DateTime? NextReviewDate { get; set; }
        public string? Notes { get; set; }
        public string? RegulatoryBody { get; set; }
        public string? ComplianceEvidence { get; set; }
        public string? NonComplianceReason { get; set; }
        public string? CorrectiveActions { get; set; }
        public DateTime? ComplianceDeadline { get; set; }
    }

    public class CreateComplianceReportDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string ReportType { get; set; } = "Compliance";
        
        [Required]
        public ComplianceReportDto ComplianceData { get; set; } = new ComplianceReportDto();
        
        public string? Comments { get; set; }
    }

    public class ComplianceReportResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public ComplianceReportDto ComplianceData { get; set; } = new ComplianceReportDto();
        public string? Comments { get; set; }
    }
}
