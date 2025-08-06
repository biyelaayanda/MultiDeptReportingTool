using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.DTOs.Analytics;
using MultiDeptReportingTool.DTOs.Export;
using MultiDeptReportingTool.Models;

namespace MultiDeptReportingTool.Services.Analytics
{
    /// <summary>
    /// Analytics service implementation for executive dashboard and advanced analytics
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Executive Dashboard Methods

        public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Set default date range if not provided (last 30 days)
                var effectiveStartDate = startDate ?? DateTime.UtcNow.AddDays(-30);
                var effectiveEndDate = endDate ?? DateTime.UtcNow;

                _logger.LogInformation($"Getting executive dashboard for date range: {effectiveStartDate:yyyy-MM-dd} to {effectiveEndDate:yyyy-MM-dd}");

                var dashboard = new ExecutiveDashboardDto
                {
                    CompanyOverview = await GetCompanyOverviewAsync(effectiveStartDate, effectiveEndDate),
                    DepartmentSummaries = await GetDepartmentSummariesAsync(effectiveStartDate, effectiveEndDate),
                    KeyMetrics = await GetKeyMetricsAsync(null, effectiveStartDate, effectiveEndDate),
                    CriticalAlerts = await GetCriticalAlertsAsync(10, effectiveStartDate, effectiveEndDate),
                    RecentTrends = await GetRecentTrendsAsync(new List<string> { "efficiency", "budget_utilization", "completion_rate" }, effectiveStartDate, effectiveEndDate),
                    TopPerformers = await GetTopPerformersAsync(5, effectiveStartDate, effectiveEndDate),
                    LastUpdated = DateTime.UtcNow
                };

                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating executive dashboard");
                throw;
            }
        }

        public async Task<CompanyOverviewDto> GetCompanyOverviewAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Set default date range if not provided
                var effectiveStartDate = startDate ?? DateTime.UtcNow.AddDays(-30);
                var effectiveEndDate = endDate ?? DateTime.UtcNow;

                // Filter reports by date range
                var reportsInRange = _context.Reports.Where(r => r.CreatedAt >= effectiveStartDate && r.CreatedAt <= effectiveEndDate);
                
                var totalReports = await reportsInRange.CountAsync();
                var totalDepartments = await _context.Departments.CountAsync();
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                
                // Calculate overall efficiency based on date range
                var completedReports = await reportsInRange.CountAsync(r => r.Status == "Approved" || r.Status == "Completed");
                var overallEfficiency = totalReports > 0 ? (decimal)completedReports / totalReports * 100 : 0;

                // Get budget data from finance reports (example)
                var totalBudget = await GetTotalBudgetAsync();
                var budgetUtilization = await GetBudgetUtilizationAsync();
                
                var pendingApprovals = await reportsInRange.CountAsync(r => r.Status == "Pending");
                var criticalIssues = await GetCriticalIssuesCountAsync(effectiveStartDate, effectiveEndDate);

                return new CompanyOverviewDto
                {
                    TotalReports = totalReports,
                    TotalDepartments = totalDepartments,
                    ActiveUsers = activeUsers,
                    OverallEfficiency = overallEfficiency,
                    TotalBudget = totalBudget,
                    BudgetUtilization = budgetUtilization,
                    PendingApprovals = pendingApprovals,
                    CriticalIssues = criticalIssues,
                    PerformanceStatus = GetPerformanceStatus(overallEfficiency, criticalIssues)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company overview");
                throw;
            }
        }

        public async Task<List<DepartmentSummaryDto>> GetDepartmentSummariesAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Set default date range if not provided
                var effectiveStartDate = startDate ?? DateTime.UtcNow.AddDays(-30);
                var effectiveEndDate = endDate ?? DateTime.UtcNow;

                var departments = await _context.Departments.ToListAsync();
                var summaries = new List<DepartmentSummaryDto>();

                foreach (var dept in departments)
                {
                    var totalReports = await _context.Reports
                        .Where(r => r.DepartmentId == dept.Id && r.CreatedAt >= effectiveStartDate && r.CreatedAt <= effectiveEndDate)
                        .CountAsync();
                    var completedReports = await _context.Reports
                        .Where(r => r.DepartmentId == dept.Id && 
                                   r.CreatedAt >= effectiveStartDate && r.CreatedAt <= effectiveEndDate &&
                                   (r.Status == "Approved" || r.Status == "Completed"))
                        .CountAsync();
                    var pendingReports = totalReports - completedReports;
                    var efficiencyScore = await CalculateEfficiencyScoreAsync(dept.Name, effectiveStartDate, effectiveEndDate);
                    var budgetUtilization = await GetDepartmentBudgetUtilizationAsync(dept.Id, effectiveStartDate, effectiveEndDate);
                    var lastActivity = await GetLastActivityAsync(dept.Id, effectiveStartDate, effectiveEndDate);

                    summaries.Add(new DepartmentSummaryDto
                    {
                        DepartmentName = dept.Name,
                        TotalReports = totalReports,
                        CompletedReports = completedReports,
                        PendingReports = pendingReports,
                        EfficiencyScore = efficiencyScore,
                        BudgetUtilization = budgetUtilization,
                        Status = GetDepartmentStatus(efficiencyScore, pendingReports),
                        TopMetrics = await GetTopMetricsForDepartmentAsync(dept.Name, effectiveStartDate, effectiveEndDate),
                        LastActivity = lastActivity
                    });
                }

                return summaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department summaries");
                throw;
            }
        }

        public async Task<List<KpiMetricDto>> GetKeyMetricsAsync(List<string> departments = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var metrics = new List<KpiMetricDto>();

                // Add financial KPIs
                metrics.AddRange(await CalculateFinancialKpisAsync(startDate, endDate));
                
                // Add operational KPIs
                metrics.AddRange(await CalculateOperationalKpisAsync(startDate, endDate));
                
                // Add HR KPIs
                metrics.AddRange(await CalculateHRKpisAsync(startDate, endDate));

                // Filter by departments if specified
                if (departments != null && departments.Any())
                {
                    metrics = metrics.Where(m => departments.Contains(m.Category) || string.IsNullOrEmpty(m.Category)).ToList();
                }

                return metrics.OrderByDescending(m => m.Priority == "High" ? 3 : m.Priority == "Medium" ? 2 : 1)
                             .ThenByDescending(m => Math.Abs(m.ChangePercentage))
                             .Take(20)
                             .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key metrics");
                throw;
            }
        }

        public async Task<List<AlertDto>> GetCriticalAlertsAsync(int limit = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Set default date range if not provided
                var effectiveStartDate = startDate ?? DateTime.UtcNow.AddDays(-30);
                var effectiveEndDate = endDate ?? DateTime.UtcNow;

                // Generate alerts based on various conditions
                var alerts = await GenerateAlertsAsync();
                
                return alerts.Where(a => (a.Severity == "Critical" || a.Severity == "Warning") &&
                                        a.CreatedAt >= effectiveStartDate && a.CreatedAt <= effectiveEndDate)
                           .OrderByDescending(a => a.Severity == "Critical" ? 2 : 1)
                           .ThenByDescending(a => a.CreatedAt)
                           .Take(limit)
                           .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting critical alerts");
                throw;
            }
        }

        public async Task<List<TrendDataDto>> GetRecentTrendsAsync(List<string> metrics, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var trends = new List<TrendDataDto>();
                var effectiveEndDate = endDate ?? DateTime.UtcNow;
                var effectiveStartDate = startDate ?? effectiveEndDate.AddDays(-30);

                _logger.LogInformation($"Getting trends for metrics: {string.Join(", ", metrics)} from {effectiveStartDate:yyyy-MM-dd} to {effectiveEndDate:yyyy-MM-dd}");

                foreach (var metric in metrics)
                {
                    var dataPoints = await GetMetricDataPointsAsync(metric, effectiveStartDate, effectiveEndDate);
                    var predictedValue = await CalculatePredictedValueAsync(dataPoints);

                    trends.Add(new TrendDataDto
                    {
                        MetricName = metric,
                        DataPoints = dataPoints,
                        ChartType = GetChartTypeForMetric(metric),
                        Color = GetColorForMetric(metric),
                        PredictedValue = predictedValue,
                        PredictionConfidence = GetPredictionConfidence(dataPoints)
                    });
                }

                return trends;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent trends");
                throw;
            }
        }

        #endregion

        #region Advanced Analytics Methods

        public async Task<List<TrendDataDto>> GetTrendAnalysisAsync(AnalyticsRequestDto request)
        {
            try
            {
                var trends = new List<TrendDataDto>();
                var startDate = request.StartDate ?? DateTime.UtcNow.AddMonths(-6);
                var endDate = request.EndDate ?? DateTime.UtcNow;

                foreach (var metric in request.Metrics)
                {
                    var dataPoints = await GetMetricDataPointsAsync(metric, startDate, endDate, request.GroupBy);
                    
                    var trend = new TrendDataDto
                    {
                        MetricName = metric,
                        DataPoints = dataPoints,
                        ChartType = request.AnalysisType == "comparison" ? "bar" : "line"
                    };

                    if (request.IncludePredictions)
                    {
                        trend.PredictedValue = await CalculatePredictedValueAsync(dataPoints);
                        trend.PredictionConfidence = GetPredictionConfidence(dataPoints);
                    }

                    trends.Add(trend);
                }

                return trends;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trend analysis");
                throw;
            }
        }

        public async Task<PerformanceComparisonDto> GetPerformanceComparisonAsync(List<string> departments, string metric, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var comparisons = new List<ComparisonItemDto>();
                var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var end = endDate ?? DateTime.UtcNow;

                foreach (var dept in departments)
                {
                    var currentScore = await GetMetricValueForDepartmentAsync(dept, metric, start, end);
                    var previousScore = await GetMetricValueForDepartmentAsync(dept, metric, start.AddMonths(-1), start);
                    var change = currentScore - previousScore;

                    comparisons.Add(new ComparisonItemDto
                    {
                        Name = dept,
                        Score = currentScore,
                        PreviousScore = previousScore,
                        Change = change,
                        Status = change >= 0 ? "Improved" : "Declined"
                    });
                }

                // Rank the departments
                var rankedComparisons = comparisons.OrderByDescending(c => c.Score).ToList();
                for (int i = 0; i < rankedComparisons.Count; i++)
                {
                    rankedComparisons[i].Rank = $"#{i + 1}";
                }

                return new PerformanceComparisonDto
                {
                    Title = $"{metric} Performance Comparison",
                    Comparisons = rankedComparisons,
                    BestPerformer = rankedComparisons.FirstOrDefault()?.Name ?? "N/A",
                    WorstPerformer = rankedComparisons.LastOrDefault()?.Name ?? "N/A",
                    AverageScore = rankedComparisons.Any() ? rankedComparisons.Average(c => c.Score) : 0,
                    Insights = GeneratePerformanceInsights(rankedComparisons)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance comparison");
                throw;
            }
        }

        public async Task<List<MultiDeptReportingTool.DTOs.Analytics.DataPointDto>> GetPredictiveAnalyticsAsync(string metric, string department, int forecastPeriods = 6)
        {
            try
            {
                // Get historical data for the last 12 months
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddMonths(-12);
                var historicalData = await GetMetricDataPointsAsync(metric, startDate, endDate, "month");

                // Simple linear regression for prediction (in a real implementation, you might use more sophisticated algorithms)
                var predictions = new List<MultiDeptReportingTool.DTOs.Analytics.DataPointDto>();
                
                if (historicalData.Count >= 3)
                {
                    var trend = CalculateLinearTrend(historicalData);
                    var lastValue = historicalData.LastOrDefault()?.Value ?? 0;
                    var lastDate = historicalData.LastOrDefault()?.Date ?? DateTime.UtcNow;

                    for (int i = 1; i <= forecastPeriods; i++)
                    {
                        var predictedDate = lastDate.AddMonths(i);
                        var predictedValue = lastValue + (trend * i);

                        predictions.Add(new MultiDeptReportingTool.DTOs.Analytics.DataPointDto
                        {
                            Date = predictedDate,
                            Value = Math.Max(0, predictedValue), // Ensure non-negative values
                            Label = $"Predicted {predictedDate:MMM yyyy}",
                            Category = "Prediction"
                        });
                    }
                }

                return predictions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting predictive analytics");
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetCustomAnalyticsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var results = new Dictionary<string, object>();

                // This is a flexible method for custom analytics queries
                // Implementation would vary based on specific requirements
                
                if (parameters.ContainsKey("query_type"))
                {
                    var queryType = parameters["query_type"].ToString();
                    
                    switch (queryType?.ToLower())
                    {
                        case "correlation":
                            results = await CalculateCorrelationAnalysisAsync(parameters);
                            break;
                        case "variance":
                            results = await CalculateVarianceAnalysisAsync(parameters);
                            break;
                        case "seasonality":
                            results = await CalculateSeasonalityAnalysisAsync(parameters);
                            break;
                        default:
                            results["error"] = "Unknown query type";
                            break;
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting custom analytics");
                return new Dictionary<string, object> { { "error", ex.Message } };
            }
        }

        #endregion

        #region KPI Calculation Methods

        public async Task<List<KpiMetricDto>> CalculateFinancialKpisAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var metrics = new List<KpiMetricDto>();
                var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var end = endDate ?? DateTime.UtcNow;

                // Revenue metrics
                var currentRevenue = await GetTotalRevenueAsync(start, end);
                var previousRevenue = await GetTotalRevenueAsync(start.AddMonths(-1), start);
                var revenueChange = previousRevenue != 0 ? ((currentRevenue - previousRevenue) / previousRevenue) * 100 : 0;

                metrics.Add(new KpiMetricDto
                {
                    Name = "Total Revenue",
                    CurrentValue = currentRevenue,
                    PreviousValue = previousRevenue,
                    TargetValue = currentRevenue * 1.1m, // 10% growth target
                    Unit = "$",
                    ChangePercentage = revenueChange,
                    Trend = revenueChange > 0 ? "Improving" : revenueChange < 0 ? "Declining" : "Stable",
                    Category = "Financial",
                    Priority = "High"
                });

                // Add more financial KPIs...
                
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating financial KPIs");
                return new List<KpiMetricDto>();
            }
        }

        public async Task<List<KpiMetricDto>> CalculateOperationalKpisAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var metrics = new List<KpiMetricDto>();
                
                // Completion rate
                var totalReports = await _context.Reports.CountAsync();
                var completedReports = await _context.Reports.CountAsync(r => r.Status == "Approved" || r.Status == "Completed");
                var completionRate = totalReports > 0 ? (decimal)completedReports / totalReports * 100 : 0;
                
                // Calculate previous period completion rate (simulate previous month)
                var previousCompletionRate = Math.Max(completionRate - 8.5m, 45m); // Simulate improvement
                var completionChangePercentage = previousCompletionRate > 0 ? 
                    (completionRate - previousCompletionRate) / previousCompletionRate * 100 : 0;

                metrics.Add(new KpiMetricDto
                {
                    Name = "Report Completion Rate",
                    CurrentValue = completionRate,
                    PreviousValue = previousCompletionRate,
                    ChangePercentage = completionChangePercentage,
                    TargetValue = 95,
                    Unit = "%",
                    Trend = completionChangePercentage > 0 ? "Improving" : completionChangePercentage < 0 ? "Declining" : "Stable",
                    Category = "Operational",
                    Priority = "High"
                });

                // Add more operational KPIs...
                
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating operational KPIs");
                return new List<KpiMetricDto>();
            }
        }

        public async Task<List<KpiMetricDto>> CalculateHRKpisAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var metrics = new List<KpiMetricDto>();
                
                // Employee engagement (placeholder calculation)
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                var totalUsers = await _context.Users.CountAsync();
                var engagementRate = totalUsers > 0 ? (decimal)activeUsers / totalUsers * 100 : 0;

                // Calculate previous engagement rate (simulate previous period)
                var previousEngagementRate = Math.Max(engagementRate - 12.4m, 75m); // Simulate improvement
                var engagementChangePercentage = previousEngagementRate > 0 ? 
                    (engagementRate - previousEngagementRate) / previousEngagementRate * 100 : 0;

                metrics.Add(new KpiMetricDto
                {
                    Name = "Employee Engagement Rate",
                    CurrentValue = engagementRate,
                    PreviousValue = previousEngagementRate,
                    ChangePercentage = engagementChangePercentage,
                    TargetValue = 85,
                    Unit = "%",
                    Trend = engagementChangePercentage > 0 ? "Improving" : engagementChangePercentage < 0 ? "Declining" : "Stable",
                    Category = "HR",
                    Priority = "Medium"
                });

                // Add more HR KPIs...
                
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating HR KPIs");
                return new List<KpiMetricDto>();
            }
        }

        public async Task<decimal> CalculateEfficiencyScoreAsync(string department, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Complex efficiency calculation based on multiple factors
                var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Name == department);
                if (dept == null) return 0;

                var completionRate = await GetCompletionRateAsync(dept.Id);
                var timeliness = await GetTimelinessScoreAsync(dept.Id);
                var quality = await GetQualityScoreAsync(dept.Id);

                // Weighted average
                var efficiencyScore = (completionRate * 0.4m) + (timeliness * 0.3m) + (quality * 0.3m);
                
                return Math.Round(efficiencyScore, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating efficiency score for department {Department}", department);
                return 0;
            }
        }

        #endregion

        #region Alert Methods

        public async Task<List<AlertDto>> GenerateAlertsAsync()
        {
            try
            {
                var alerts = new List<AlertDto>();

                // Check for overdue reports (using ReportPeriodEnd as due date)
                var overdueReports = await _context.Reports
                    .Where(r => r.ReportPeriodEnd < DateTime.UtcNow && r.Status != "Completed" && r.Status != "Approved")
                    .Include(r => r.Department)
                    .ToListAsync();

                foreach (var report in overdueReports)
                {
                    alerts.Add(new AlertDto
                    {
                        Title = "Overdue Report",
                        Message = $"Report '{report.Title}' is overdue",
                        Severity = "Warning",
                        Department = report.Department?.Name ?? "Unknown",
                        CreatedAt = DateTime.UtcNow,
                        ActionRequired = "Complete the report immediately",
                        ResponsibleUser = "biyelaayanda3@gmail.com" // Using the provided email for testing
                    });
                }

                // Check for budget overruns
                // Implementation would depend on your budget tracking logic

                // Check for low performance metrics
                // Implementation would depend on your performance criteria

                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating alerts");
                return new List<AlertDto>();
            }
        }

        public async Task<AlertDto> CreateAlertAsync(string title, string message, string severity, string department, string responsibleUser)
        {
            try
            {
                var alert = new AlertDto
                {
                    Title = title,
                    Message = message,
                    Severity = severity,
                    Department = department,
                    CreatedAt = DateTime.UtcNow,
                    ResponsibleUser = responsibleUser,
                    IsRead = false
                };

                // In a real implementation, you might save this to a database
                // For now, we'll just return the created alert

                return alert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert");
                throw;
            }
        }

        public async Task<bool> MarkAlertAsReadAsync(int alertId, string userId)
        {
            try
            {
                // Implementation would update the alert in the database
                // For now, return true as a placeholder
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking alert as read");
                return false;
            }
        }

        public async Task<bool> DismissAlertAsync(int alertId, string userId)
        {
            try
            {
                // Implementation would dismiss the alert in the database
                // For now, return true as a placeholder
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing alert");
                return false;
            }
        }

        #endregion

        #region Business Intelligence Methods

        public async Task<Dictionary<string, decimal>> GetRevenueAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var analytics = new Dictionary<string, decimal>();
                
                // Placeholder revenue calculations
                // In a real implementation, this would analyze actual financial data
                analytics["total_revenue"] = await GetTotalRevenueAsync(startDate, endDate);
                analytics["recurring_revenue"] = analytics["total_revenue"] * 0.7m;
                analytics["new_revenue"] = analytics["total_revenue"] * 0.3m;
                analytics["revenue_growth_rate"] = 5.2m; // Placeholder percentage

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue analytics");
                return new Dictionary<string, decimal>();
            }
        }

        public async Task<Dictionary<string, decimal>> GetCostAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var analytics = new Dictionary<string, decimal>();
                
                // Placeholder cost calculations
                analytics["total_costs"] = 250000m; // Placeholder
                analytics["operational_costs"] = 150000m;
                analytics["personnel_costs"] = 100000m;
                analytics["cost_per_report"] = 500m;

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cost analytics");
                return new Dictionary<string, decimal>();
            }
        }

        public async Task<Dictionary<string, int>> GetProductivityMetricsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var metrics = new Dictionary<string, int>();
                
                var totalReports = await _context.Reports.CountAsync();
                var completedReports = await _context.Reports.CountAsync(r => r.Status == "Approved" || r.Status == "Completed");
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);

                metrics["total_reports"] = totalReports;
                metrics["completed_reports"] = completedReports;
                metrics["reports_per_user"] = activeUsers > 0 ? totalReports / activeUsers : 0;
                metrics["productivity_score"] = totalReports > 0 ? (completedReports * 100 / totalReports) : 0;

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting productivity metrics");
                return new Dictionary<string, int>();
            }
        }

        public async Task<List<object>> GetBenchmarkingDataAsync(string metric, List<string> departments)
        {
            try
            {
                var benchmarkData = new List<object>();

                foreach (var dept in departments)
                {
                    var value = await GetMetricValueForDepartmentAsync(dept, metric);
                    benchmarkData.Add(new
                    {
                        Department = dept,
                        Value = value,
                        Metric = metric,
                        BenchmarkScore = CalculateBenchmarkScore(value, metric)
                    });
                }

                return benchmarkData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting benchmarking data");
                return new List<object>();
            }
        }

        #endregion

        #region Data Aggregation Methods

        public async Task<Dictionary<string, object>> AggregateDataByPeriodAsync(string metric, string period, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var aggregatedData = new Dictionary<string, object>();
                var start = startDate ?? DateTime.UtcNow.AddMonths(-12);
                var end = endDate ?? DateTime.UtcNow;

                // Implementation would vary based on the metric and period
                // For now, providing a basic structure

                aggregatedData["metric"] = metric;
                aggregatedData["period"] = period;
                aggregatedData["start_date"] = start;
                aggregatedData["end_date"] = end;
                aggregatedData["data_points"] = await GetMetricDataPointsAsync(metric, start, end, period);

                return aggregatedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aggregating data by period");
                return new Dictionary<string, object>();
            }
        }

        public async Task<List<object>> GetTopPerformersAsync(string metric, int limit = 10)
        {
            try
            {
                var departments = await _context.Departments.ToListAsync();
                var performers = new List<object>();

                foreach (var dept in departments)
                {
                    var value = await GetMetricValueForDepartmentAsync(dept.Name, metric);
                    performers.Add(new
                    {
                        Department = dept.Name,
                        Value = value,
                        Metric = metric
                    });
                }

                return performers.OrderByDescending(p => ((dynamic)p).Value).Take(limit).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top performers");
                return new List<object>();
            }
        }

        public async Task<List<object>> GetBottomPerformersAsync(string metric, int limit = 10)
        {
            try
            {
                var topPerformers = await GetTopPerformersAsync(metric, 100); // Get all, then reverse
                return topPerformers.OrderBy(p => ((dynamic)p).Value).Take(limit).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bottom performers");
                return new List<object>();
            }
        }

        public async Task<Dictionary<string, decimal>> GetVarianceAnalysisAsync(string metric, DateTime? compareDate = null)
        {
            try
            {
                var analysis = new Dictionary<string, decimal>();
                var currentDate = DateTime.UtcNow;
                var comparisonDate = compareDate ?? currentDate.AddMonths(-1);

                var currentValue = await GetMetricValueAsync(metric, currentDate.AddDays(-30), currentDate);
                var previousValue = await GetMetricValueAsync(metric, comparisonDate.AddDays(-30), comparisonDate);

                var variance = currentValue - previousValue;
                var variancePercentage = previousValue != 0 ? (variance / previousValue) * 100 : 0;

                analysis["current_value"] = currentValue;
                analysis["previous_value"] = previousValue;
                analysis["variance"] = variance;
                analysis["variance_percentage"] = variancePercentage;

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting variance analysis");
                return new Dictionary<string, decimal>();
            }
        }

        #endregion

        #region Helper Methods

        private async Task<decimal> GetTotalBudgetAsync()
        {
            // Placeholder implementation
            // In a real system, this would aggregate budget data from financial reports
            return await Task.FromResult(1000000m);
        }

        private async Task<decimal> GetBudgetUtilizationAsync()
        {
            // Placeholder implementation
            return await Task.FromResult(75.5m);
        }

        private async Task<int> GetCriticalIssuesCountAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            // Set default date range if not provided
            var effectiveEndDate = endDate ?? DateTime.UtcNow;
            var effectiveStartDate = startDate ?? effectiveEndDate.AddDays(-30);

            // Count critical issues based on various criteria within date range
            var overdueReports = await _context.Reports.CountAsync(r => 
                r.CreatedAt >= effectiveStartDate && 
                r.CreatedAt <= effectiveEndDate &&
                r.ReportPeriodEnd < DateTime.UtcNow && 
                r.Status != "Approved" && 
                r.Status != "Published");
            
            // Add other critical issue counts here
            return overdueReports;
        }

        private string GetPerformanceStatus(decimal efficiency, int criticalIssues)
        {
            if (efficiency < 70 || criticalIssues > 10) return "Critical";
            if (efficiency < 85 || criticalIssues > 5) return "Warning";
            return "Good";
        }

        private async Task<decimal> GetDepartmentBudgetUtilizationAsync(int departmentId, DateTime? startDate = null, DateTime? endDate = null)
        {
            // Placeholder implementation
            return await Task.FromResult(80.0m + (departmentId % 20)); // Random-ish value for demo
        }

        private async Task<DateTime> GetLastActivityAsync(int departmentId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var lastReport = await _context.Reports
                .Where(r => r.DepartmentId == departmentId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            return lastReport?.CreatedAt ?? DateTime.UtcNow.AddDays(-30);
        }

        private string GetDepartmentStatus(decimal efficiency, int pendingReports)
        {
            if (efficiency < 70 || pendingReports > 10) return "Critical";
            if (efficiency < 85 || pendingReports > 5) return "Warning";
            return "Active";
        }

        private async Task<List<KpiMetricDto>> GetTopMetricsForDepartmentAsync(string departmentName, DateTime? startDate = null, DateTime? endDate = null)
        {
            // Return top 3 metrics for the department
            var metrics = new List<KpiMetricDto>();
            
            var efficiency = await CalculateEfficiencyScoreAsync(departmentName);
            metrics.Add(new KpiMetricDto
            {
                Name = "Efficiency Score",
                CurrentValue = efficiency,
                TargetValue = 90,
                Unit = "%",
                Category = departmentName,
                Priority = "High"
            });

            return metrics.Take(3).ToList();
        }

        private async Task<List<MultiDeptReportingTool.DTOs.Analytics.DataPointDto>> GetMetricDataPointsAsync(string metric, DateTime startDate, DateTime endDate, string groupBy = "day")
        {
            try
            {
                var dataPoints = new List<MultiDeptReportingTool.DTOs.Analytics.DataPointDto>();
                var current = startDate;

                while (current <= endDate)
                {
                    var value = await GetMetricValueAsync(metric, current, current.AddDays(1));
                    dataPoints.Add(new MultiDeptReportingTool.DTOs.Analytics.DataPointDto
                    {
                        Date = current,
                        Value = value,
                        Label = current.ToString("MMM dd"),
                        Category = metric
                    });

                    current = groupBy switch
                    {
                        "week" => current.AddDays(7),
                        "month" => current.AddMonths(1),
                        "quarter" => current.AddMonths(3),
                        "year" => current.AddYears(1),
                        _ => current.AddDays(1)
                    };
                }

                return dataPoints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metric data points");
                return new List<DataPointDto>();
            }
        }

        private async Task<decimal> GetMetricValueAsync(string metric, DateTime startDate, DateTime endDate)
        {
            // Placeholder implementation - would calculate actual metric values
            var random = new Random();
            return await Task.FromResult((decimal)(random.NextDouble() * 100));
        }

        private async Task<decimal> GetMetricValueForDepartmentAsync(string department, string metric, DateTime? startDate = null, DateTime? endDate = null)
        {
            // Placeholder implementation
            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Name == department);
            if (dept == null) return 0;

            var random = new Random(dept.Id + metric.GetHashCode());
            return (decimal)(random.NextDouble() * 100);
        }

        private async Task<decimal> CalculatePredictedValueAsync(List<MultiDeptReportingTool.DTOs.Analytics.DataPointDto> dataPoints)
        {
            if (dataPoints.Count < 2) return 0;

            var trend = CalculateLinearTrend(dataPoints);
            var lastValue = dataPoints.LastOrDefault()?.Value ?? 0;
            
            return await Task.FromResult(lastValue + trend);
        }

        private decimal CalculateLinearTrend(List<MultiDeptReportingTool.DTOs.Analytics.DataPointDto> dataPoints)
        {
            if (dataPoints.Count < 2) return 0;

            // Simple linear regression slope calculation
            var n = dataPoints.Count;
            var sumX = 0m;
            var sumY = 0m;
            var sumXY = 0m;
            var sumX2 = 0m;

            for (int i = 0; i < n; i++)
            {
                sumX += i;
                sumY += dataPoints[i].Value;
                sumXY += i * dataPoints[i].Value;
                sumX2 += i * i;
            }

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return slope;
        }

        private string GetChartTypeForMetric(string metric)
        {
            return metric.ToLower() switch
            {
                "efficiency" => "line",
                "budget_utilization" => "area",
                "completion_rate" => "bar",
                _ => "line"
            };
        }

        private string GetColorForMetric(string metric)
        {
            return metric.ToLower() switch
            {
                "efficiency" => "#28a745",
                "budget_utilization" => "#007bff",
                "completion_rate" => "#ffc107",
                _ => "#6c757d"
            };
        }

        private string GetPredictionConfidence(List<MultiDeptReportingTool.DTOs.Analytics.DataPointDto> dataPoints)
        {
            if (dataPoints.Count < 5) return "Low";
            if (dataPoints.Count < 10) return "Medium";
            return "High";
        }

        private List<string> GeneratePerformanceInsights(List<ComparisonItemDto> comparisons)
        {
            var insights = new List<string>();

            if (comparisons.Any())
            {
                var bestPerformer = comparisons.First();
                var worstPerformer = comparisons.Last();
                
                insights.Add($"{bestPerformer.Name} leads with a score of {bestPerformer.Score:F1}");
                
                if (bestPerformer.Change > 0)
                {
                    insights.Add($"{bestPerformer.Name} showed improvement of {bestPerformer.Change:F1}");
                }

                if (worstPerformer.Change < 0)
                {
                    insights.Add($"{worstPerformer.Name} needs attention with a decline of {Math.Abs(worstPerformer.Change):F1}");
                }

                var improvingCount = comparisons.Count(c => c.Change > 0);
                insights.Add($"{improvingCount} out of {comparisons.Count} departments showed improvement");
            }

            return insights;
        }

        private async Task<decimal> GetCompletionRateAsync(int departmentId)
        {
            var total = await _context.Reports.CountAsync(r => r.DepartmentId == departmentId);
            var completed = await _context.Reports.CountAsync(r => r.DepartmentId == departmentId && (r.Status == "Approved" || r.Status == "Completed"));
            return total > 0 ? (decimal)completed / total * 100 : 0;
        }

        private async Task<decimal> GetTimelinessScoreAsync(int departmentId)
        {
            // Calculate based on reports completed on time (using SubmittedAt vs ReportPeriodEnd)
            var onTimeReports = await _context.Reports
                .CountAsync(r => r.DepartmentId == departmentId && 
                                r.Status == "Approved" && 
                                r.SubmittedAt.HasValue &&
                                r.SubmittedAt <= r.ReportPeriodEnd);
            
            var totalCompleted = await _context.Reports
                .CountAsync(r => r.DepartmentId == departmentId && r.Status == "Approved");

            return totalCompleted > 0 ? (decimal)onTimeReports / totalCompleted * 100 : 0;
        }

        private async Task<decimal> GetQualityScoreAsync(int departmentId)
        {
            // Placeholder quality score calculation
            // In a real implementation, this might be based on review scores, rework rates, etc.
            return await Task.FromResult(85.0m + (departmentId % 15)); // Demo value
        }

        private async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            // Calculate revenue based on completed reports and their data
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;
            
            var completedReports = await _context.Reports
                .Where(r => (r.Status == "Approved" || r.Status == "Completed") && 
                           r.CreatedAt >= start && r.CreatedAt <= end)
                .CountAsync();
            
            // Base revenue calculation: $5,000 per completed report + variable monthly bonus
            var baseRevenue = completedReports * 5000m;
            var monthsPassed = (decimal)((end - start).Days / 30.0);
            var monthlyBonus = monthsPassed * 50000m;
            
            // Add some variation based on the time period to show different values
            var timeVariation = (decimal)(Math.Sin(end.DayOfYear / 365.0 * 2 * Math.PI) * 25000.0);
            
            return baseRevenue + monthlyBonus + timeVariation;
        }

        private decimal CalculateBenchmarkScore(decimal value, string metric)
        {
            // Placeholder benchmark calculation
            // In a real implementation, this would compare against industry standards
            return value > 80 ? 100 : value > 60 ? 75 : value > 40 ? 50 : 25;
        }

        private async Task<Dictionary<string, object>> CalculateCorrelationAnalysisAsync(Dictionary<string, object> parameters)
        {
            // Placeholder correlation analysis
            return await Task.FromResult(new Dictionary<string, object>
            {
                { "correlation_coefficient", 0.85m },
                { "significance", "High" },
                { "analysis", "Strong positive correlation detected" }
            });
        }

        private async Task<Dictionary<string, object>> CalculateVarianceAnalysisAsync(Dictionary<string, object> parameters)
        {
            // Placeholder variance analysis
            return await Task.FromResult(new Dictionary<string, object>
            {
                { "variance", 12.5m },
                { "standard_deviation", 3.54m },
                { "coefficient_of_variation", 15.2m }
            });
        }

        private async Task<Dictionary<string, object>> CalculateSeasonalityAnalysisAsync(Dictionary<string, object> parameters)
        {
            // Placeholder seasonality analysis
            return await Task.FromResult(new Dictionary<string, object>
            {
                { "seasonal_pattern", "Quarterly peaks in Q1 and Q3" },
                { "seasonal_strength", 0.65m },
                { "trend_strength", 0.23m }
            });
        }

        #endregion

        public async Task<List<TopPerformerDto>> GetTopPerformersAsync(int limit = 5, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Set default date range if not provided
                var effectiveEndDate = endDate ?? DateTime.UtcNow;
                var effectiveStartDate = startDate ?? effectiveEndDate.AddDays(-30);

                _logger.LogInformation($"Getting top performers for date range: {effectiveStartDate:yyyy-MM-dd} to {effectiveEndDate:yyyy-MM-dd}");

                // Get user performance data based on report completion within date range
                var reports = await _context.Reports
                    .Where(r => r.Status == "Approved" && 
                                r.CreatedAt >= effectiveStartDate && 
                                r.CreatedAt <= effectiveEndDate)
                    .Include(r => r.CreatedByUser)
                    .Include(r => r.Department)
                    .ToListAsync();

                _logger.LogInformation($"Found {reports.Count} approved reports in date range");

                // Group and calculate on the client side
                var userPerformance = reports
                    .Where(r => r.CreatedByUserId > 0 && r.CreatedByUser != null)
                    .GroupBy(r => new { r.CreatedByUserId, r.DepartmentId })
                    .Select(g => new
                    {
                        UserId = g.Key.CreatedByUserId,
                        DepartmentId = g.Key.DepartmentId,
                        CompletedReports = g.Count(),
                        AverageCompletionTime = g.Where(r => r.ApprovedAt.HasValue)
                            .Select(r => (r.ApprovedAt.Value - r.CreatedAt).Days)
                            .DefaultIfEmpty(0)
                            .Average(),
                        Efficiency = (decimal)g.Count() * 100 / Math.Max(g.Count() + 1, 1),
                        User = g.First().CreatedByUser,
                        Department = g.First().Department
                    })
                    .OrderByDescending(u => u.CompletedReports)
                    .ThenByDescending(u => u.Efficiency)
                    .Take(limit)
                    .ToList();

                _logger.LogInformation($"Debug - Found {userPerformance.Count} user performance records");

                // Create top performers list
                var topPerformers = new List<TopPerformerDto>();
                var rank = 1;

                foreach (var userPerf in userPerformance)
                {
                    if (userPerf.User != null && userPerf.Department != null)
                    {
                        var trendValue = userPerf.Efficiency > 80 ? "Up" : userPerf.Efficiency > 60 ? "Stable" : "Down";
                        
                        topPerformers.Add(new TopPerformerDto
                        {
                            UserId = userPerf.UserId,
                            UserName = $"{userPerf.User.FirstName} {userPerf.User.LastName}".Trim(),
                            DepartmentName = userPerf.Department.Name,
                            CompletedReports = userPerf.CompletedReports,
                            AverageCompletionTime = Math.Max((decimal)userPerf.AverageCompletionTime, 0.1m),
                            Efficiency = userPerf.Efficiency,
                            Rank = rank++,
                            Trend = trendValue
                        });
                    }
                }

                return topPerformers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top performers");
                return new List<TopPerformerDto>();
            }
        }
    }
}
