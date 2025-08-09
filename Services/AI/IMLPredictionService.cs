using MultiDeptReportingTool.DTOs.Analytics;

namespace MultiDeptReportingTool.Services.AI
{
    /// <summary>
    /// Machine Learning and Predictive Analytics Service Interface
    /// Provides AI-powered insights and predictions for business intelligence
    /// </summary>
    public interface IMLPredictionService
    {
        #region Predictive Analytics
        
        /// <summary>
        /// Generate advanced time series predictions using multiple algorithms
        /// </summary>
        Task<PredictionResultDto> GenerateAdvancedPredictionAsync(string metric, string department, int forecastPeriods = 6, string algorithm = "auto");
        
        /// <summary>
        /// Predict department performance trends
        /// </summary>
        Task<List<DepartmentPerformancePredictionDto>> PredictDepartmentPerformanceAsync(List<string> departments, int forecastMonths = 6);
        
        /// <summary>
        /// Generate budget forecasting with confidence intervals
        /// </summary>
        Task<BudgetForecastDto> GenerateBudgetForecastAsync(string department, int forecastMonths = 12);
        
        /// <summary>
        /// Predict resource requirements based on historical patterns
        /// </summary>
        Task<ResourcePredictionDto> PredictResourceRequirementsAsync(string department, DateTime targetDate);

        #endregion

        #region Anomaly Detection
        
        /// <summary>
        /// Detect anomalies in real-time data streams
        /// </summary>
        Task<List<AnomalyDto>> DetectAnomaliesAsync(string metric, DateTime? startDate = null, DateTime? endDate = null);
        
        /// <summary>
        /// Generate automated alerts for unusual patterns
        /// </summary>
        Task<List<AlertDto>> GenerateAnomalyAlertsAsync();
        
        /// <summary>
        /// Analyze data quality and suggest improvements
        /// </summary>
        Task<DataQualityAssessmentDto> AssessDataQualityAsync(string dataSource);

        #endregion

        #region Pattern Recognition
        
        /// <summary>
        /// Identify seasonal patterns and trends
        /// </summary>
        Task<SeasonalPatternDto> IdentifySeasonalPatternsAsync(string metric, int lookbackMonths = 24);
        
        /// <summary>
        /// Find correlations between different metrics
        /// </summary>
        Task<List<CorrelationDto>> FindMetricCorrelationsAsync(List<string> metrics);
        
        /// <summary>
        /// Cluster departments by performance characteristics
        /// </summary>
        Task<List<DepartmentClusterDto>> ClusterDepartmentsByPerformanceAsync();

        #endregion

        #region Natural Language Processing
        
        /// <summary>
        /// Analyze sentiment in reports and feedback
        /// </summary>
        Task<SentimentAnalysisDto> AnalyzeSentimentAsync(List<string> textData);
        
        /// <summary>
        /// Extract key insights from unstructured data
        /// </summary>
        Task<List<InsightDto>> ExtractInsightsFromTextAsync(List<string> documents);
        
        /// <summary>
        /// Generate automated report summaries
        /// </summary>
        Task<string> GenerateReportSummaryAsync(List<string> reportData);

        #endregion

        #region Advanced Analytics
        
        /// <summary>
        /// Perform multivariate analysis on complex datasets
        /// </summary>
        Task<MultivariateAnalysisDto> PerformMultivariateAnalysisAsync(Dictionary<string, List<decimal>> datasets);
        
        /// <summary>
        /// Generate what-if scenarios for business planning
        /// </summary>
        Task<List<ScenarioAnalysisDto>> GenerateWhatIfScenariosAsync(Dictionary<string, decimal> parameters);
        
        /// <summary>
        /// Optimize resource allocation using AI algorithms
        /// </summary>
        Task<ResourceOptimizationDto> OptimizeResourceAllocationAsync(List<string> departments, Dictionary<string, decimal> constraints);

        #endregion
    }
}
