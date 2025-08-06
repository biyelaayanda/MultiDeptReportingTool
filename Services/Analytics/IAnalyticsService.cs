using MultiDeptReportingTool.DTOs.Analytics;
using MultiDeptReportingTool.DTOs.Export;

namespace MultiDeptReportingTool.Services.Analytics
{
    /// <summary>
    /// Analytics service interface for executive dashboard and advanced analytics
    /// </summary>
    public interface IAnalyticsService
    {
        // Executive Dashboard Methods
        Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<CompanyOverviewDto> GetCompanyOverviewAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<DepartmentSummaryDto>> GetDepartmentSummariesAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<KpiMetricDto>> GetKeyMetricsAsync(List<string> departments = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<AlertDto>> GetCriticalAlertsAsync(int limit = 10, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<TrendDataDto>> GetRecentTrendsAsync(List<string> metrics, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<TopPerformerDto>> GetTopPerformersAsync(int limit = 5, DateTime? startDate = null, DateTime? endDate = null);

        // Advanced Analytics Methods
        Task<List<TrendDataDto>> GetTrendAnalysisAsync(AnalyticsRequestDto request);
        Task<PerformanceComparisonDto> GetPerformanceComparisonAsync(List<string> departments, string metric, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<MultiDeptReportingTool.DTOs.Analytics.DataPointDto>> GetPredictiveAnalyticsAsync(string metric, string department, int forecastPeriods = 6);
        Task<Dictionary<string, object>> GetCustomAnalyticsAsync(Dictionary<string, object> parameters);

        // KPI and Metrics Methods
        Task<List<KpiMetricDto>> CalculateFinancialKpisAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<KpiMetricDto>> CalculateOperationalKpisAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<KpiMetricDto>> CalculateHRKpisAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> CalculateEfficiencyScoreAsync(string department, DateTime? startDate = null, DateTime? endDate = null);

        // Alert and Notification Methods
        Task<List<AlertDto>> GenerateAlertsAsync();
        Task<AlertDto> CreateAlertAsync(string title, string message, string severity, string department, string responsibleUser);
        Task<bool> MarkAlertAsReadAsync(int alertId, string userId);
        Task<bool> DismissAlertAsync(int alertId, string userId);

        // Business Intelligence Methods
        Task<Dictionary<string, decimal>> GetRevenueAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, decimal>> GetCostAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, int>> GetProductivityMetricsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<object>> GetBenchmarkingDataAsync(string metric, List<string> departments);

        // Data Aggregation Methods
        Task<Dictionary<string, object>> AggregateDataByPeriodAsync(string metric, string period, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<object>> GetTopPerformersAsync(string metric, int limit = 10);
        Task<List<object>> GetBottomPerformersAsync(string metric, int limit = 10);
        Task<Dictionary<string, decimal>> GetVarianceAnalysisAsync(string metric, DateTime? compareDate = null);
    }
}
