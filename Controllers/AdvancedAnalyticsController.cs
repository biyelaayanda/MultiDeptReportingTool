using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.DTOs.Analytics;
using MultiDeptReportingTool.Services.AI;
using MultiDeptReportingTool.Services.Analytics;

namespace MultiDeptReportingTool.Controllers
{
    /// <summary>
    /// Advanced Analytics Controller with AI/ML Integration
    /// Phase 3: Advanced Analytics & AI Features
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdvancedAnalyticsController : ControllerBase
    {
        private readonly IMLPredictionService _mlService;
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<AdvancedAnalyticsController> _logger;

        public AdvancedAnalyticsController(
            IMLPredictionService mlService,
            IAnalyticsService analyticsService,
            ILogger<AdvancedAnalyticsController> logger)
        {
            _mlService = mlService;
            _analyticsService = analyticsService;
            _logger = logger;
        }

        #region AI-Powered Predictions

        /// <summary>
        /// Generate advanced predictive analytics using machine learning
        /// </summary>
        [HttpPost("predictions/advanced")]
        public async Task<ActionResult<PredictionResultDto>> GenerateAdvancedPrediction(
            [FromBody] AdvancedPredictionRequestDto request)
        {
            try
            {
                _logger.LogInformation($"Generating advanced prediction for {request.Metric} in {request.Department}");
                
                var result = await _mlService.GenerateAdvancedPredictionAsync(
                    request.Metric, 
                    request.Department, 
                    request.ForecastPeriods,
                    request.Algorithm);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating advanced prediction");
                return StatusCode(500, new { error = "Failed to generate prediction", details = ex.Message });
            }
        }

        /// <summary>
        /// Predict department performance trends with AI analysis
        /// </summary>
        [HttpPost("predictions/department-performance")]
        public async Task<ActionResult<List<DepartmentPerformancePredictionDto>>> PredictDepartmentPerformance(
            [FromBody] DepartmentPerformanceRequestDto request)
        {
            try
            {
                var predictions = await _mlService.PredictDepartmentPerformanceAsync(
                    request.Departments, 
                    request.ForecastMonths);

                return Ok(predictions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting department performance");
                return StatusCode(500, new { error = "Failed to predict performance", details = ex.Message });
            }
        }

        /// <summary>
        /// Generate intelligent budget forecasts with confidence intervals
        /// </summary>
        [HttpGet("predictions/budget-forecast/{department}")]
        public async Task<ActionResult<BudgetForecastDto>> GenerateBudgetForecast(
            string department,
            [FromQuery] int forecastMonths = 12)
        {
            try
            {
                var forecast = await _mlService.GenerateBudgetForecastAsync(department, forecastMonths);
                return Ok(forecast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating budget forecast for {department}");
                return StatusCode(500, new { error = "Failed to generate forecast", details = ex.Message });
            }
        }

        /// <summary>
        /// Predict future resource requirements using AI
        /// </summary>
        [HttpGet("predictions/resource-requirements/{department}")]
        public async Task<ActionResult<ResourcePredictionDto>> PredictResourceRequirements(
            string department,
            [FromQuery] DateTime? targetDate = null)
        {
            try
            {
                var target = targetDate ?? DateTime.UtcNow.AddMonths(6);
                var prediction = await _mlService.PredictResourceRequirementsAsync(department, target);
                
                return Ok(prediction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error predicting resource requirements for {department}");
                return StatusCode(500, new { error = "Failed to predict resources", details = ex.Message });
            }
        }

        #endregion

        #region Anomaly Detection & Monitoring

        /// <summary>
        /// Detect anomalies in real-time data using AI algorithms
        /// </summary>
        [HttpGet("anomalies/detect/{metric}")]
        public async Task<ActionResult<List<AnomalyDto>>> DetectAnomalies(
            string metric,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var anomalies = await _mlService.DetectAnomaliesAsync(metric, startDate, endDate);
                return Ok(anomalies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error detecting anomalies for {metric}");
                return StatusCode(500, new { error = "Failed to detect anomalies", details = ex.Message });
            }
        }

        /// <summary>
        /// Generate automated alerts for unusual patterns
        /// </summary>
        [HttpGet("anomalies/alerts")]
        public async Task<ActionResult<List<AlertDto>>> GenerateAnomalyAlerts()
        {
            try
            {
                var alerts = await _mlService.GenerateAnomalyAlertsAsync();
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating anomaly alerts");
                return StatusCode(500, new { error = "Failed to generate alerts", details = ex.Message });
            }
        }

        /// <summary>
        /// Assess data quality using AI analysis
        /// </summary>
        [HttpGet("data-quality/assess/{dataSource}")]
        public async Task<ActionResult<DataQualityAssessmentDto>> AssessDataQuality(string dataSource)
        {
            try
            {
                var assessment = await _mlService.AssessDataQualityAsync(dataSource);
                return Ok(assessment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assessing data quality for {dataSource}");
                return StatusCode(500, new { error = "Failed to assess data quality", details = ex.Message });
            }
        }

        #endregion

        #region Pattern Recognition & Intelligence

        /// <summary>
        /// Identify seasonal patterns using advanced algorithms
        /// </summary>
        [HttpGet("patterns/seasonal/{metric}")]
        public async Task<ActionResult<SeasonalPatternDto>> IdentifySeasonalPatterns(
            string metric,
            [FromQuery] int lookbackMonths = 24)
        {
            try
            {
                var patterns = await _mlService.IdentifySeasonalPatternsAsync(metric, lookbackMonths);
                return Ok(patterns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error identifying seasonal patterns for {metric}");
                return StatusCode(500, new { error = "Failed to identify patterns", details = ex.Message });
            }
        }

        /// <summary>
        /// Find correlations between different metrics using AI
        /// </summary>
        [HttpPost("patterns/correlations")]
        public async Task<ActionResult<List<CorrelationDto>>> FindMetricCorrelations(
            [FromBody] CorrelationRequestDto request)
        {
            try
            {
                var correlations = await _mlService.FindMetricCorrelationsAsync(request.Metrics);
                return Ok(correlations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding metric correlations");
                return StatusCode(500, new { error = "Failed to find correlations", details = ex.Message });
            }
        }

        /// <summary>
        /// Cluster departments by performance characteristics using ML
        /// </summary>
        [HttpGet("patterns/department-clusters")]
        public async Task<ActionResult<List<DepartmentClusterDto>>> ClusterDepartmentsByPerformance()
        {
            try
            {
                var clusters = await _mlService.ClusterDepartmentsByPerformanceAsync();
                return Ok(clusters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clustering departments");
                return StatusCode(500, new { error = "Failed to cluster departments", details = ex.Message });
            }
        }

        #endregion

        #region Natural Language Processing

        /// <summary>
        /// Analyze sentiment in reports and feedback using NLP
        /// </summary>
        [HttpPost("nlp/sentiment-analysis")]
        public async Task<ActionResult<SentimentAnalysisDto>> AnalyzeSentiment(
            [FromBody] SentimentAnalysisRequestDto request)
        {
            try
            {
                var analysis = await _mlService.AnalyzeSentimentAsync(request.TextData);
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing sentiment");
                return StatusCode(500, new { error = "Failed to analyze sentiment", details = ex.Message });
            }
        }

        /// <summary>
        /// Extract insights from unstructured text using AI
        /// </summary>
        [HttpPost("nlp/extract-insights")]
        public async Task<ActionResult<List<InsightDto>>> ExtractInsights(
            [FromBody] InsightExtractionRequestDto request)
        {
            try
            {
                var insights = await _mlService.ExtractInsightsFromTextAsync(request.Documents);
                return Ok(insights);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting insights");
                return StatusCode(500, new { error = "Failed to extract insights", details = ex.Message });
            }
        }

        /// <summary>
        /// Generate automated report summaries using AI
        /// </summary>
        [HttpPost("nlp/generate-summary")]
        public async Task<ActionResult<string>> GenerateReportSummary(
            [FromBody] ReportSummaryRequestDto request)
        {
            try
            {
                var summary = await _mlService.GenerateReportSummaryAsync(request.ReportData);
                return Ok(new { summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report summary");
                return StatusCode(500, new { error = "Failed to generate summary", details = ex.Message });
            }
        }

        #endregion

        #region Advanced Analytics & Optimization

        /// <summary>
        /// Perform multivariate analysis on complex datasets
        /// </summary>
        [HttpPost("advanced/multivariate-analysis")]
        public async Task<ActionResult<MultivariateAnalysisDto>> PerformMultivariateAnalysis(
            [FromBody] MultivariateAnalysisRequestDto request)
        {
            try
            {
                var analysis = await _mlService.PerformMultivariateAnalysisAsync(request.Datasets);
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing multivariate analysis");
                return StatusCode(500, new { error = "Failed to perform analysis", details = ex.Message });
            }
        }

        /// <summary>
        /// Generate what-if scenarios for business planning
        /// </summary>
        [HttpPost("advanced/scenario-analysis")]
        public async Task<ActionResult<List<ScenarioAnalysisDto>>> GenerateWhatIfScenarios(
            [FromBody] ScenarioAnalysisRequestDto request)
        {
            try
            {
                var scenarios = await _mlService.GenerateWhatIfScenariosAsync(request.Parameters);
                return Ok(scenarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating scenarios");
                return StatusCode(500, new { error = "Failed to generate scenarios", details = ex.Message });
            }
        }

        /// <summary>
        /// Optimize resource allocation using AI algorithms
        /// </summary>
        [HttpPost("advanced/optimize-resources")]
        public async Task<ActionResult<ResourceOptimizationDto>> OptimizeResourceAllocation(
            [FromBody] ResourceOptimizationRequestDto request)
        {
            try
            {
                var optimization = await _mlService.OptimizeResourceAllocationAsync(
                    request.Departments, 
                    request.Constraints);
                    
                return Ok(optimization);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing resource allocation");
                return StatusCode(500, new { error = "Failed to optimize resources", details = ex.Message });
            }
        }

        #endregion

        #region Comprehensive AI Dashboard

        /// <summary>
        /// Get comprehensive AI-powered analytics dashboard
        /// </summary>
        [HttpGet("dashboard/ai-insights")]
        public async Task<ActionResult<AIInsightsDashboardDto>> GetAIInsightsDashboard(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var end = endDate ?? DateTime.UtcNow;

                // Gather AI insights from multiple sources
                var dashboard = new AIInsightsDashboardDto
                {
                    GeneratedAt = DateTime.UtcNow,
                    DateRange = new { start, end },
                    
                    // Predictive insights
                    KeyPredictions = await _mlService.PredictDepartmentPerformanceAsync(
                        new[] { "Sales", "Marketing", "Operations", "HR" }.ToList(), 6),
                    
                    // Anomaly detection
                    RecentAnomalies = await _mlService.DetectAnomaliesAsync("efficiency", start, end),
                    
                    // Pattern recognition
                    SeasonalInsights = await _mlService.IdentifySeasonalPatternsAsync("revenue", 12),
                    
                    // Performance clustering
                    DepartmentClusters = await _mlService.ClusterDepartmentsByPerformanceAsync(),
                    
                    // AI-generated recommendations
                    AIRecommendations = await GenerateAIRecommendations(),
                    
                    // Data quality overview
                    DataQualityScores = await GetDataQualityOverview()
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI insights dashboard");
                return StatusCode(500, new { error = "Failed to generate AI dashboard", details = ex.Message });
            }
        }

        /// <summary>
        /// Get AI-powered performance recommendations
        /// </summary>
        [HttpGet("recommendations/ai-generated")]
        public async Task<ActionResult<List<AIRecommendationDto>>> GetAIRecommendations()
        {
            try
            {
                var recommendations = await GenerateAIRecommendations();
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI recommendations");
                return StatusCode(500, new { error = "Failed to generate recommendations", details = ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        private async Task<List<AIRecommendationDto>> GenerateAIRecommendations()
        {
            try
            {
                var recommendations = new List<AIRecommendationDto>();

                // Analyze recent performance data to generate recommendations
                var departments = new[] { "Sales", "Marketing", "Operations", "HR" };
                
                foreach (var dept in departments)
                {
                    var predictions = await _mlService.PredictDepartmentPerformanceAsync(new[] { dept }.ToList(), 3);
                    var prediction = predictions.FirstOrDefault();
                    
                    if (prediction != null)
                    {
                        var riskLevel = prediction.RiskScore > 0.7m ? "High" : 
                                       prediction.RiskScore > 0.4m ? "Medium" : "Low";
                        
                        recommendations.Add(new AIRecommendationDto
                        {
                            Title = $"{dept} Department Performance Optimization",
                            Description = $"AI analysis suggests {(prediction.RiskScore > 0.5m ? "immediate attention" : "continued monitoring")} for {dept}",
                            Priority = riskLevel,
                            Category = "Performance",
                            Department = dept,
                            ConfidenceScore = 0.85m,
                            ExpectedImpact = prediction.RiskScore > 0.5m ? "High" : "Medium",
                            ActionItems = prediction.RecommendedActions,
                            GeneratedAt = DateTime.UtcNow
                        });
                    }
                }

                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI recommendations");
                return new List<AIRecommendationDto>();
            }
        }

        private async Task<Dictionary<string, decimal>> GetDataQualityOverview()
        {
            try
            {
                var dataSources = new[] { "reports", "users", "departments", "activities" };
                var qualityScores = new Dictionary<string, decimal>();
                
                foreach (var source in dataSources)
                {
                    var assessment = await _mlService.AssessDataQualityAsync(source);
                    qualityScores[source] = assessment.OverallQualityScore;
                }
                
                return qualityScores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data quality overview");
                return new Dictionary<string, decimal>();
            }
        }

        #endregion
    }

    #region Request DTOs

    public class AdvancedPredictionRequestDto
    {
        public string Metric { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int ForecastPeriods { get; set; } = 6;
        public string Algorithm { get; set; } = "auto";
    }

    public class DepartmentPerformanceRequestDto
    {
        public List<string> Departments { get; set; } = new();
        public int ForecastMonths { get; set; } = 6;
    }

    public class CorrelationRequestDto
    {
        public List<string> Metrics { get; set; } = new();
    }

    public class SentimentAnalysisRequestDto
    {
        public List<string> TextData { get; set; } = new();
    }

    public class InsightExtractionRequestDto
    {
        public List<string> Documents { get; set; } = new();
    }

    public class ReportSummaryRequestDto
    {
        public List<string> ReportData { get; set; } = new();
    }

    public class MultivariateAnalysisRequestDto
    {
        public Dictionary<string, List<decimal>> Datasets { get; set; } = new();
    }

    public class ScenarioAnalysisRequestDto
    {
        public Dictionary<string, decimal> Parameters { get; set; } = new();
    }

    public class ResourceOptimizationRequestDto
    {
        public List<string> Departments { get; set; } = new();
        public Dictionary<string, decimal> Constraints { get; set; } = new();
    }

    #endregion

    #region Response DTOs

    public class AIInsightsDashboardDto
    {
        public DateTime GeneratedAt { get; set; }
        public object DateRange { get; set; } = new();
        public List<DepartmentPerformancePredictionDto> KeyPredictions { get; set; } = new();
        public List<AnomalyDto> RecentAnomalies { get; set; } = new();
        public SeasonalPatternDto SeasonalInsights { get; set; } = new();
        public List<DepartmentClusterDto> DepartmentClusters { get; set; } = new();
        public List<AIRecommendationDto> AIRecommendations { get; set; } = new();
        public Dictionary<string, decimal> DataQualityScores { get; set; } = new();
    }

    public class AIRecommendationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal ConfidenceScore { get; set; }
        public string ExpectedImpact { get; set; } = string.Empty;
        public List<string> ActionItems { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    #endregion
}
