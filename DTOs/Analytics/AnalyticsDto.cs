using System.ComponentModel.DataAnnotations;

namespace MultiDeptReportingTool.DTOs.Analytics
{
    /// <summary>
    /// Executive dashboard overview DTO
    /// </summary>
    public class ExecutiveDashboardDto
    {
        public CompanyOverviewDto CompanyOverview { get; set; } = new CompanyOverviewDto();
        public List<DepartmentSummaryDto> DepartmentSummaries { get; set; } = new List<DepartmentSummaryDto>();
        public List<KpiMetricDto> KeyMetrics { get; set; } = new List<KpiMetricDto>();
        public List<AlertDto> CriticalAlerts { get; set; } = new List<AlertDto>();
        public List<TrendDataDto> RecentTrends { get; set; } = new List<TrendDataDto>();
        public List<TopPerformerDto> TopPerformers { get; set; } = new List<TopPerformerDto>();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class CompanyOverviewDto
    {
        public int TotalReports { get; set; }
        public int TotalDepartments { get; set; }
        public int ActiveUsers { get; set; }
        public decimal OverallEfficiency { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal BudgetUtilization { get; set; }
        public int PendingApprovals { get; set; }
        public int CriticalIssues { get; set; }
        public string PerformanceStatus { get; set; } = "Good"; // Good, Warning, Critical
    }

    public class DepartmentSummaryDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalReports { get; set; }
        public int CompletedReports { get; set; }
        public int PendingReports { get; set; }
        public decimal EfficiencyScore { get; set; }
        public decimal BudgetUtilization { get; set; }
        public string Status { get; set; } = "Active"; // Active, Warning, Critical
        public List<KpiMetricDto> TopMetrics { get; set; } = new List<KpiMetricDto>();
        public DateTime LastActivity { get; set; }
    }

    public class KpiMetricDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal TargetValue { get; set; }
        public decimal PreviousValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Trend { get; set; } = "Stable"; // Improving, Declining, Stable
        public decimal ChangePercentage { get; set; }
        public string Category { get; set; } = string.Empty; // Financial, Operational, HR, etc.
        public string Priority { get; set; } = "Medium"; // High, Medium, Low
    }

    public class AlertDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "Info"; // Critical, Warning, Info
        public string Department { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string ActionRequired { get; set; } = string.Empty;
        public string ResponsibleUser { get; set; } = string.Empty;
    }

    public class TrendDataDto
    {
        public string MetricName { get; set; } = string.Empty;
        public List<DataPointDto> DataPoints { get; set; } = new List<DataPointDto>();
        public string ChartType { get; set; } = "line"; // line, bar, area
        public string Color { get; set; } = "#007bff";
        public decimal PredictedValue { get; set; }
        public string PredictionConfidence { get; set; } = "Medium"; // High, Medium, Low
    }

    public class DataPointDto
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// Advanced analytics request DTO
    /// </summary>
    public class AnalyticsRequestDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<string> Departments { get; set; } = new List<string>();
        public List<string> Metrics { get; set; } = new List<string>();
        public string GroupBy { get; set; } = "month"; // day, week, month, quarter, year
        public string AnalysisType { get; set; } = "trend"; // trend, comparison, forecast
        public bool IncludePredictions { get; set; } = false;
        public int ForecastPeriods { get; set; } = 3;
    }

    /// <summary>
    /// Performance comparison DTO
    /// </summary>
    public class PerformanceComparisonDto
    {
        public string Title { get; set; } = string.Empty;
        public List<ComparisonItemDto> Comparisons { get; set; } = new List<ComparisonItemDto>();
        public string BestPerformer { get; set; } = string.Empty;
        public string WorstPerformer { get; set; } = string.Empty;
        public decimal AverageScore { get; set; }
        public List<string> Insights { get; set; } = new List<string>();
    }

    public class ComparisonItemDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public decimal PreviousScore { get; set; }
        public decimal Change { get; set; }
        public string Rank { get; set; } = string.Empty;
        public string Status { get; set; } = "Normal";
    }

    public class TopPerformerDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int CompletedReports { get; set; }
        public decimal AverageCompletionTime { get; set; } // In days
        public decimal Efficiency { get; set; } // Percentage
        public int Rank { get; set; }
        public string Trend { get; set; } = "Stable"; // Up, Down, Stable
    }
}
