namespace MultiDeptReportingTool.DTOs.Analytics
{
    /// <summary>
    /// Advanced prediction result with multiple algorithms and confidence metrics
    /// </summary>
    public class PredictionResultDto
    {
        public string Metric { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
        public List<DataPointDto> PredictedValues { get; set; } = new();
        public decimal ConfidenceScore { get; set; }
        public decimal MeanAbsoluteError { get; set; }
        public Dictionary<string, decimal> ModelMetrics { get; set; } = new();
        public List<string> Insights { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Department performance prediction with trend analysis
    /// </summary>
    public class DepartmentPerformancePredictionDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public List<DataPointDto> PerformanceProjection { get; set; } = new();
        public decimal PredictedEfficiency { get; set; }
        public decimal BudgetUtilizationForecast { get; set; }
        public decimal RiskScore { get; set; }
        public List<string> RecommendedActions { get; set; } = new();
        public Dictionary<string, decimal> KeyMetricsPrediction { get; set; } = new();
    }

    /// <summary>
    /// Advanced budget forecasting with confidence intervals
    /// </summary>
    public class BudgetForecastDto
    {
        public string Department { get; set; } = string.Empty;
        public List<BudgetProjectionDto> MonthlyProjections { get; set; } = new();
        public decimal TotalPredictedBudget { get; set; }
        public decimal UpperConfidenceLimit { get; set; }
        public decimal LowerConfidenceLimit { get; set; }
        public decimal VariabilityScore { get; set; }
        public List<string> BudgetRisks { get; set; } = new();
        public List<string> CostOptimizationSuggestions { get; set; } = new();
    }

    public class BudgetProjectionDto
    {
        public DateTime Month { get; set; }
        public decimal PredictedAmount { get; set; }
        public decimal ConfidenceInterval { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resource requirement predictions
    /// </summary>
    public class ResourcePredictionDto
    {
        public string Department { get; set; } = string.Empty;
        public DateTime TargetDate { get; set; }
        public int PredictedStaffNeeds { get; set; }
        public decimal PredictedBudgetRequirement { get; set; }
        public Dictionary<string, int> SkillGaps { get; set; } = new();
        public List<string> TrainingRecommendations { get; set; } = new();
        public decimal WorkloadPrediction { get; set; }
    }

    /// <summary>
    /// Anomaly detection results
    /// </summary>
    public class AnomalyDto
    {
        public string MetricName { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public decimal ActualValue { get; set; }
        public decimal ExpectedValue { get; set; }
        public decimal DeviationScore { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string PossibleCause { get; set; } = string.Empty;
        public List<string> RecommendedActions { get; set; } = new();
        public bool IsResolved { get; set; }
    }

    /// <summary>
    /// Data quality assessment results
    /// </summary>
    public class DataQualityAssessmentDto
    {
        public string DataSource { get; set; } = string.Empty;
        public decimal OverallQualityScore { get; set; }
        public Dictionary<string, decimal> QualityMetrics { get; set; } = new();
        public List<string> IssuesFound { get; set; } = new();
        public List<string> ImprovementSuggestions { get; set; } = new();
        public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Seasonal pattern analysis results
    /// </summary>
    public class SeasonalPatternDto
    {
        public string MetricName { get; set; } = string.Empty;
        public Dictionary<string, decimal> SeasonalFactors { get; set; } = new();
        public List<DataPointDto> TrendLine { get; set; } = new();
        public decimal Seasonality { get; set; }
        public string DominantPattern { get; set; } = string.Empty;
        public List<string> PatternInsights { get; set; } = new();
    }

    /// <summary>
    /// Correlation analysis between metrics
    /// </summary>
    public class CorrelationDto
    {
        public string MetricA { get; set; } = string.Empty;
        public string MetricB { get; set; } = string.Empty;
        public decimal CorrelationCoefficient { get; set; }
        public string CorrelationStrength { get; set; } = string.Empty;
        public string Interpretation { get; set; } = string.Empty;
        public decimal PValue { get; set; }
        public bool IsStatisticallySignificant { get; set; }
    }

    /// <summary>
    /// Department clustering results
    /// </summary>
    public class DepartmentClusterDto
    {
        public string ClusterName { get; set; } = string.Empty;
        public List<string> Departments { get; set; } = new();
        public Dictionary<string, decimal> ClusterCharacteristics { get; set; } = new();
        public string PerformanceLevel { get; set; } = string.Empty;
        public List<string> CommonTraits { get; set; } = new();
        public List<string> ImprovementOpportunities { get; set; } = new();
    }

    /// <summary>
    /// Sentiment analysis results
    /// </summary>
    public class SentimentAnalysisDto
    {
        public decimal OverallSentiment { get; set; }
        public Dictionary<string, int> SentimentDistribution { get; set; } = new();
        public List<string> PositiveKeywords { get; set; } = new();
        public List<string> NegativeKeywords { get; set; } = new();
        public List<string> KeyThemes { get; set; } = new();
        public decimal ConfidenceScore { get; set; }
    }

    /// <summary>
    /// AI-generated insights from text data
    /// </summary>
    public class InsightDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal ImportanceScore { get; set; }
        public List<string> SupportingEvidence { get; set; } = new();
        public List<string> ActionableRecommendations { get; set; } = new();
    }

    /// <summary>
    /// Multivariate analysis results
    /// </summary>
    public class MultivariateAnalysisDto
    {
        public Dictionary<string, decimal> VariableImportance { get; set; } = new();
        public List<CorrelationDto> Correlations { get; set; } = new();
        public Dictionary<string, decimal> PrincipalComponents { get; set; } = new();
        public List<string> KeyFindings { get; set; } = new();
        public decimal ModelAccuracy { get; set; }
    }

    /// <summary>
    /// Scenario analysis for what-if planning
    /// </summary>
    public class ScenarioAnalysisDto
    {
        public string ScenarioName { get; set; } = string.Empty;
        public Dictionary<string, decimal> InputParameters { get; set; } = new();
        public Dictionary<string, decimal> PredictedOutcomes { get; set; } = new();
        public decimal ProbabilityScore { get; set; }
        public string ImpactAssessment { get; set; } = string.Empty;
        public List<string> RiskFactors { get; set; } = new();
        public List<string> Opportunities { get; set; } = new();
    }

    /// <summary>
    /// Resource optimization recommendations
    /// </summary>
    public class ResourceOptimizationDto
    {
        public Dictionary<string, decimal> OptimalAllocation { get; set; } = new();
        public decimal ExpectedImprovement { get; set; }
        public Dictionary<string, decimal> CostSavings { get; set; } = new();
        public List<string> ReallocationSuggestions { get; set; } = new();
        public decimal ImplementationComplexity { get; set; }
        public string RecommendedTimeline { get; set; } = string.Empty;
    }
}
