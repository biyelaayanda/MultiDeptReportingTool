using System.ComponentModel.DataAnnotations;

namespace MultiDeptReportingTool.DTOs.DepartmentSpecific
{
    /// <summary>
    /// Finance Department specific report DTO for budget tracking and expenditure reporting
    /// </summary>
    public class FinanceReportDto
    {
        [Required]
        public string ReportPeriod { get; set; } = string.Empty; // "Monthly", "Quarterly", "Annual"
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total expenditure must be greater than 0")]
        public decimal TotalExpenditure { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Budget allocation must be greater than 0")]
        public decimal BudgetAllocation { get; set; }
        
        public decimal VarianceAmount => BudgetAllocation - TotalExpenditure;
        public decimal VariancePercentage => BudgetAllocation > 0 ? (VarianceAmount / BudgetAllocation) * 100 : 0;
        
        public List<ExpenditureCategoryDto> ExpenditureCategories { get; set; } = new List<ExpenditureCategoryDto>();
        public string? BudgetJustification { get; set; }
        public List<string> CostCenters { get; set; } = new List<string>();
        
        [Required]
        public DateTime ReportingPeriodStart { get; set; }
        
        [Required]
        public DateTime ReportingPeriodEnd { get; set; }
        
        public string? FinancialNotes { get; set; }
        public string? RecommendedActions { get; set; }
    }

    public class ExpenditureCategoryDto
    {
        [Required]
        public string CategoryName { get; set; } = string.Empty;
        
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }
        
        [Required]
        [Range(0, double.MaxValue)]
        public decimal BudgetedAmount { get; set; }
        
        public decimal Variance => BudgetedAmount - Amount;
        public decimal VariancePercentage => BudgetedAmount > 0 ? (Variance / BudgetedAmount) * 100 : 0;
        
        public string? Description { get; set; }
        public string? CostCenter { get; set; }
    }

    public class CreateFinanceReportDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string ReportType { get; set; } = "Finance";
        
        [Required]
        public FinanceReportDto FinanceData { get; set; } = new FinanceReportDto();
        
        public string? Comments { get; set; }
    }

    public class FinanceReportResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public FinanceReportDto FinanceData { get; set; } = new FinanceReportDto();
        public string? Comments { get; set; }
    }
}
