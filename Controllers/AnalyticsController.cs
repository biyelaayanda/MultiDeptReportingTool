using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MultiDeptReportingTool.Services.Analytics;
using MultiDeptReportingTool.DTOs.Analytics;

namespace MultiDeptReportingTool.Controllers
{
    /// <summary>
    /// Analytics controller for executive dashboard and advanced analytics
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }

        #region Executive Dashboard Endpoints

        /// <summary>
        /// Get complete executive dashboard data
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<ExecutiveDashboardDto>> GetExecutiveDashboard(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var dashboard = await _analyticsService.GetExecutiveDashboardAsync(startDate, endDate);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting executive dashboard");
                return StatusCode(500, new { message = "Error retrieving executive dashboard", error = ex.Message });
            }
        }

        /// <summary>
        /// Get company overview metrics
        /// </summary>
        [HttpGet("overview")]
        public async Task<ActionResult<CompanyOverviewDto>> GetCompanyOverview()
        {
            try
            {
                var overview = await _analyticsService.GetCompanyOverviewAsync();
                return Ok(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company overview");
                return StatusCode(500, new { message = "Error retrieving company overview", error = ex.Message });
            }
        }

        /// <summary>
        /// Get department summaries
        /// </summary>
        [HttpGet("departments/summary")]
        public async Task<ActionResult<List<DepartmentSummaryDto>>> GetDepartmentSummaries()
        {
            try
            {
                var summaries = await _analyticsService.GetDepartmentSummariesAsync();
                return Ok(summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department summaries");
                return StatusCode(500, new { message = "Error retrieving department summaries", error = ex.Message });
            }
        }

        /// <summary>
        /// Get key performance metrics
        /// </summary>
        [HttpGet("metrics")]
        public async Task<ActionResult<List<KpiMetricDto>>> GetKeyMetrics(
            [FromQuery] List<string> departments = null)
        {
            try
            {
                var metrics = await _analyticsService.GetKeyMetricsAsync(departments);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key metrics");
                return StatusCode(500, new { message = "Error retrieving key metrics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get critical alerts
        /// </summary>
        [HttpGet("alerts")]
        public async Task<ActionResult<List<AlertDto>>> GetCriticalAlerts(
            [FromQuery] int limit = 10)
        {
            try
            {
                var alerts = await _analyticsService.GetCriticalAlertsAsync(limit);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting critical alerts");
                return StatusCode(500, new { message = "Error retrieving critical alerts", error = ex.Message });
            }
        }

        /// <summary>
        /// Get recent trends data
        /// </summary>
        [HttpGet("trends")]
        public async Task<ActionResult<List<TrendDataDto>>> GetRecentTrends(
            [FromQuery] List<string> metrics,
            [FromQuery] int days = 30)
        {
            try
            {
                if (metrics == null || !metrics.Any())
                {
                    metrics = new List<string> { "efficiency", "budget_utilization", "completion_rate" };
                }

                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-days);
                var trends = await _analyticsService.GetRecentTrendsAsync(metrics, startDate, endDate);
                return Ok(trends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent trends");
                return StatusCode(500, new { message = "Error retrieving recent trends", error = ex.Message });
            }
        }

        #endregion

        #region Advanced Analytics Endpoints

        /// <summary>
        /// Get trend analysis based on request parameters
        /// </summary>
        [HttpPost("analysis/trends")]
        public async Task<ActionResult<List<TrendDataDto>>> GetTrendAnalysis(
            [FromBody] AnalyticsRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Analytics request is required" });
                }

                var analysis = await _analyticsService.GetTrendAnalysisAsync(request);
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trend analysis");
                return StatusCode(500, new { message = "Error retrieving trend analysis", error = ex.Message });
            }
        }

        /// <summary>
        /// Get performance comparison between departments
        /// </summary>
        [HttpPost("analysis/comparison")]
        public async Task<ActionResult<PerformanceComparisonDto>> GetPerformanceComparison(
            [FromBody] PerformanceComparisonRequestDto request)
        {
            try
            {
                if (request == null || !request.Departments.Any() || string.IsNullOrWhiteSpace(request.Metric))
                {
                    return BadRequest(new { message = "Departments and metric are required" });
                }

                var comparison = await _analyticsService.GetPerformanceComparisonAsync(
                    request.Departments, request.Metric, request.StartDate, request.EndDate);
                return Ok(comparison);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance comparison");
                return StatusCode(500, new { message = "Error retrieving performance comparison", error = ex.Message });
            }
        }

        /// <summary>
        /// Get predictive analytics for a specific metric and department
        /// </summary>
        [HttpGet("predictions/{department}/{metric}")]
        public async Task<ActionResult<List<MultiDeptReportingTool.DTOs.Analytics.DataPointDto>>> GetPredictiveAnalytics(
            string department,
            string metric,
            [FromQuery] int forecastPeriods = 6)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(department) || string.IsNullOrWhiteSpace(metric))
                {
                    return BadRequest(new { message = "Department and metric are required" });
                }

                var predictions = await _analyticsService.GetPredictiveAnalyticsAsync(metric, department, forecastPeriods);
                return Ok(predictions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting predictive analytics");
                return StatusCode(500, new { message = "Error retrieving predictive analytics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get custom analytics based on parameters
        /// </summary>
        [HttpPost("analysis/custom")]
        public async Task<ActionResult<Dictionary<string, object>>> GetCustomAnalytics(
            [FromBody] Dictionary<string, object> parameters)
        {
            try
            {
                if (parameters == null || !parameters.Any())
                {
                    return BadRequest(new { message = "Analysis parameters are required" });
                }

                var results = await _analyticsService.GetCustomAnalyticsAsync(parameters);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting custom analytics");
                return StatusCode(500, new { message = "Error retrieving custom analytics", error = ex.Message });
            }
        }

        #endregion

        #region KPI Endpoints

        /// <summary>
        /// Get financial KPIs
        /// </summary>
        [HttpGet("kpis/financial")]
        public async Task<ActionResult<List<KpiMetricDto>>> GetFinancialKpis(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var kpis = await _analyticsService.CalculateFinancialKpisAsync(startDate, endDate);
                return Ok(kpis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting financial KPIs");
                return StatusCode(500, new { message = "Error retrieving financial KPIs", error = ex.Message });
            }
        }

        /// <summary>
        /// Get operational KPIs
        /// </summary>
        [HttpGet("kpis/operational")]
        public async Task<ActionResult<List<KpiMetricDto>>> GetOperationalKpis(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var kpis = await _analyticsService.CalculateOperationalKpisAsync(startDate, endDate);
                return Ok(kpis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting operational KPIs");
                return StatusCode(500, new { message = "Error retrieving operational KPIs", error = ex.Message });
            }
        }

        /// <summary>
        /// Get HR KPIs
        /// </summary>
        [HttpGet("kpis/hr")]
        public async Task<ActionResult<List<KpiMetricDto>>> GetHrKpis(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var kpis = await _analyticsService.CalculateHRKpisAsync(startDate, endDate);
                return Ok(kpis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting HR KPIs");
                return StatusCode(500, new { message = "Error retrieving HR KPIs", error = ex.Message });
            }
        }

        /// <summary>
        /// Calculate efficiency score for a department
        /// </summary>
        [HttpGet("efficiency/{department}")]
        public async Task<ActionResult<decimal>> GetEfficiencyScore(
            string department,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(department))
                {
                    return BadRequest(new { message = "Department is required" });
                }

                var score = await _analyticsService.CalculateEfficiencyScoreAsync(department, startDate, endDate);
                return Ok(new { department, efficiencyScore = score });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting efficiency score");
                return StatusCode(500, new { message = "Error calculating efficiency score", error = ex.Message });
            }
        }

        #endregion

        #region Alert Management Endpoints

        /// <summary>
        /// Create a new alert
        /// </summary>
        [HttpPost("alerts")]
        public async Task<ActionResult<AlertDto>> CreateAlert([FromBody] CreateAlertRequestDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { message = "Title and message are required" });
                }

                var alert = await _analyticsService.CreateAlertAsync(
                    request.Title, request.Message, request.Severity, request.Department, 
                    request.ResponsibleUser ?? "biyelaayanda3@gmail.com");

                return Ok(alert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert");
                return StatusCode(500, new { message = "Error creating alert", error = ex.Message });
            }
        }

        /// <summary>
        /// Mark an alert as read
        /// </summary>
        [HttpPut("alerts/{alertId}/read")]
        public async Task<ActionResult> MarkAlertAsRead(int alertId)
        {
            try
            {
                var userId = User.Identity?.Name ?? "current_user";
                var success = await _analyticsService.MarkAlertAsReadAsync(alertId, userId);
                
                if (success)
                {
                    return Ok(new { message = "Alert marked as read" });
                }
                
                return NotFound(new { message = "Alert not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking alert as read");
                return StatusCode(500, new { message = "Error marking alert as read", error = ex.Message });
            }
        }

        /// <summary>
        /// Dismiss an alert
        /// </summary>
        [HttpDelete("alerts/{alertId}")]
        public async Task<ActionResult> DismissAlert(int alertId)
        {
            try
            {
                var userId = User.Identity?.Name ?? "current_user";
                var success = await _analyticsService.DismissAlertAsync(alertId, userId);
                
                if (success)
                {
                    return Ok(new { message = "Alert dismissed" });
                }
                
                return NotFound(new { message = "Alert not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing alert");
                return StatusCode(500, new { message = "Error dismissing alert", error = ex.Message });
            }
        }

        #endregion

        #region Business Intelligence Endpoints

        /// <summary>
        /// Get revenue analytics
        /// </summary>
        [HttpGet("business-intelligence/revenue")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetRevenueAnalytics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var analytics = await _analyticsService.GetRevenueAnalyticsAsync(startDate, endDate);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue analytics");
                return StatusCode(500, new { message = "Error retrieving revenue analytics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get cost analytics
        /// </summary>
        [HttpGet("business-intelligence/costs")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetCostAnalytics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var analytics = await _analyticsService.GetCostAnalyticsAsync(startDate, endDate);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cost analytics");
                return StatusCode(500, new { message = "Error retrieving cost analytics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get productivity metrics
        /// </summary>
        [HttpGet("business-intelligence/productivity")]
        public async Task<ActionResult<Dictionary<string, int>>> GetProductivityMetrics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var metrics = await _analyticsService.GetProductivityMetricsAsync(startDate, endDate);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting productivity metrics");
                return StatusCode(500, new { message = "Error retrieving productivity metrics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get benchmarking data
        /// </summary>
        [HttpPost("business-intelligence/benchmarking")]
        public async Task<ActionResult<List<object>>> GetBenchmarkingData(
            [FromBody] BenchmarkingRequestDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Metric) || !request.Departments.Any())
                {
                    return BadRequest(new { message = "Metric and departments are required" });
                }

                var data = await _analyticsService.GetBenchmarkingDataAsync(request.Metric, request.Departments);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting benchmarking data");
                return StatusCode(500, new { message = "Error retrieving benchmarking data", error = ex.Message });
            }
        }

        #endregion

        #region Data Aggregation Endpoints

        /// <summary>
        /// Get aggregated data by time period
        /// </summary>
        [HttpGet("aggregation/{metric}")]
        public async Task<ActionResult<Dictionary<string, object>>> GetAggregatedData(
            string metric,
            [FromQuery] string period = "month",
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(metric))
                {
                    return BadRequest(new { message = "Metric is required" });
                }

                var data = await _analyticsService.AggregateDataByPeriodAsync(metric, period, startDate, endDate);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting aggregated data");
                return StatusCode(500, new { message = "Error retrieving aggregated data", error = ex.Message });
            }
        }

        /// <summary>
        /// Get top performers for a metric
        /// </summary>
        [HttpGet("performers/top/{metric}")]
        public async Task<ActionResult<List<object>>> GetTopPerformers(
            string metric,
            [FromQuery] int limit = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(metric))
                {
                    return BadRequest(new { message = "Metric is required" });
                }

                var performers = await _analyticsService.GetTopPerformersAsync(metric, limit);
                return Ok(performers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top performers");
                return StatusCode(500, new { message = "Error retrieving top performers", error = ex.Message });
            }
        }

        /// <summary>
        /// Get bottom performers for a metric
        /// </summary>
        [HttpGet("performers/bottom/{metric}")]
        public async Task<ActionResult<List<object>>> GetBottomPerformers(
            string metric,
            [FromQuery] int limit = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(metric))
                {
                    return BadRequest(new { message = "Metric is required" });
                }

                var performers = await _analyticsService.GetBottomPerformersAsync(metric, limit);
                return Ok(performers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bottom performers");
                return StatusCode(500, new { message = "Error retrieving bottom performers", error = ex.Message });
            }
        }

        /// <summary>
        /// Get variance analysis for a metric
        /// </summary>
        [HttpGet("variance/{metric}")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetVarianceAnalysis(
            string metric,
            [FromQuery] DateTime? compareDate = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(metric))
                {
                    return BadRequest(new { message = "Metric is required" });
                }

                var analysis = await _analyticsService.GetVarianceAnalysisAsync(metric, compareDate);
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting variance analysis");
                return StatusCode(500, new { message = "Error retrieving variance analysis", error = ex.Message });
            }
        }

        /// <summary>
        /// Debug endpoint to check top performers data
        /// </summary>
        [HttpGet("debug/top-performers")]
        public async Task<ActionResult> DebugTopPerformers()
        {
            try
            {
                var topPerformers = await _analyticsService.GetTopPerformersAsync(10);
                
                // Also get raw user and report data for debugging
                var debugData = new
                {
                    TopPerformers = topPerformers,
                    TopPerformersCount = topPerformers.Count,
                    Message = topPerformers.Count == 0 ? "No top performers found - checking raw data..." : "Top performers found"
                };

                return Ok(debugData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in debug top performers");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        #endregion
    }

    #region Request DTOs

    public class PerformanceComparisonRequestDto
    {
        public List<string> Departments { get; set; } = new List<string>();
        public string Metric { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class CreateAlertRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "Info";
        public string Department { get; set; } = string.Empty;
        public string ResponsibleUser { get; set; } = string.Empty;
    }

    public class BenchmarkingRequestDto
    {
        public string Metric { get; set; } = string.Empty;
        public List<string> Departments { get; set; } = new List<string>();
    }

    #endregion
}
