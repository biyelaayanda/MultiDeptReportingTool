namespace MultiDeptReportingTool.DTOs.DisasterRecovery
{
    public class BackupDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public BackupType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public BackupStatus Status { get; set; }
        public long SizeBytes { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string ChecksumMd5 { get; set; } = string.Empty;
        public string ChecksumSha256 { get; set; } = string.Empty;
        public bool IsEncrypted { get; set; }
        public int CompressionRatio { get; set; }
        public TimeSpan Duration { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public List<string> IncludedTables { get; set; } = new();
        public List<string> ExcludedTables { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        public BackupRetentionPolicy RetentionPolicy { get; set; } = new();
    }

    public class BackupScheduleDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public BackupType BackupType { get; set; }
        public string CronExpression { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public DateTime NextRunTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        public BackupStatus? LastRunStatus { get; set; }
        public int MaxRetentionDays { get; set; } = 30;
        public int MaxBackupCount { get; set; } = 10;
        public List<string> NotificationEmails { get; set; } = new();
        public bool CompressBackup { get; set; } = true;
        public bool EncryptBackup { get; set; } = true;
        public string BackupLocation { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class BackupValidationDto
    {
        public string BackupId { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public bool ChecksumValid { get; set; }
        public bool FileExists { get; set; }
        public bool CanRestore { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public DateTime ValidationDate { get; set; }
        public TimeSpan ValidationDuration { get; set; }
    }

    public class BackupRetentionPolicy
    {
        public int DailyRetentionDays { get; set; } = 7;
        public int WeeklyRetentionWeeks { get; set; } = 4;
        public int MonthlyRetentionMonths { get; set; } = 12;
        public int YearlyRetentionYears { get; set; } = 7;
        public bool AutoDeleteExpired { get; set; } = true;
    }

    public class RestoreOptions
    {
        public bool RestoreSchema { get; set; } = true;
        public bool RestoreData { get; set; } = true;
        public bool RestoreIndexes { get; set; } = true;
        public bool RestorePermissions { get; set; } = true;
        public List<string> SpecificTables { get; set; } = new();
        public DateTime? PointInTime { get; set; }
        public string TargetDatabase { get; set; } = string.Empty;
        public bool VerifyAfterRestore { get; set; } = true;
        public bool CreateBackupBeforeRestore { get; set; } = true;
    }

    public class DisasterRecoveryPlanDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DisasterType DisasterType { get; set; }
        public int Priority { get; set; } = 1; // 1=Critical, 5=Low
        public TimeSpan RecoveryTimeObjective { get; set; } // RTO
        public TimeSpan RecoveryPointObjective { get; set; } // RPO
        public List<RecoveryStepDto> RecoverySteps { get; set; } = new();
        public List<string> ResponsiblePersons { get; set; } = new();
        public List<string> ContactInformation { get; set; } = new();
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime? LastTested { get; set; }
        public bool IsActive { get; set; } = true;
        public string Version { get; set; } = "1.0";
    }

    public class RecoveryStepDto
    {
        public int StepNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ResponsibleRole { get; set; } = string.Empty;
        public TimeSpan EstimatedDuration { get; set; }
        public List<string> Prerequisites { get; set; } = new();
        public List<string> Commands { get; set; } = new();
        public string VerificationCriteria { get; set; } = string.Empty;
        public bool IsAutomated { get; set; }
        public string AutomationScript { get; set; } = string.Empty;
    }

    public class IncidentDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IncidentType Type { get; set; }
        public IncidentSeverity Severity { get; set; }
        public IncidentStatus Status { get; set; }
        public DateTime ReportedDate { get; set; }
        public DateTime? DetectedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public string ReportedBy { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public List<string> AffectedSystems { get; set; } = new();
        public string ImpactAssessment { get; set; } = string.Empty;
        public string RootCause { get; set; } = string.Empty;
        public string Resolution { get; set; } = string.Empty;
        public List<string> ActionsPerformed { get; set; } = new();
        public TimeSpan? DowntimeDuration { get; set; }
        public decimal? EstimatedCost { get; set; }
        public bool RequiresDrActivation { get; set; }
        public string DrPlanId { get; set; } = string.Empty;
    }

    public class IncidentResponseDto
    {
        public string IncidentId { get; set; } = string.Empty;
        public List<ResponseActionDto> Actions { get; set; } = new();
        public List<string> Stakeholders { get; set; } = new();
        public List<string> CommunicationLog { get; set; } = new();
        public TimeSpan ResponseTime { get; set; }
        public TimeSpan ResolutionTime { get; set; }
        public bool SlaCompliant { get; set; }
        public string PostIncidentReport { get; set; } = string.Empty;
    }

    public class ResponseActionDto
    {
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
    }

    public class SystemHealthDto
    {
        public DateTime Timestamp { get; set; }
        public HealthStatus OverallStatus { get; set; }
        public List<ComponentHealthDto> Components { get; set; } = new();
        public SystemMetricsDto Metrics { get; set; } = new();
        public List<string> Alerts { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public decimal AvailabilityPercentage { get; set; }
        public TimeSpan SystemUptime { get; set; }
    }

    public class ComponentHealthDto
    {
        public string ComponentName { get; set; } = string.Empty;
        public HealthStatus Status { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
    }

    public class SystemMetricsDto
    {
        public double CpuUsagePercent { get; set; }
        public double MemoryUsagePercent { get; set; }
        public double DiskUsagePercent { get; set; }
        public double NetworkLatencyMs { get; set; }
        public int ActiveConnections { get; set; }
        public int DatabaseConnections { get; set; }
        public double RequestsPerSecond { get; set; }
        public double ErrorRate { get; set; }
    }

    public class HealthCheckDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public HealthCheckType Type { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public TimeSpan Interval { get; set; }
        public TimeSpan Timeout { get; set; }
        public int RetryCount { get; set; } = 3;
        public bool IsEnabled { get; set; } = true;
        public HealthStatus LastStatus { get; set; }
        public DateTime LastCheck { get; set; }
        public string LastError { get; set; } = string.Empty;
    }

    public class FailoverStatusDto
    {
        public string Id { get; set; } = string.Empty;
        public FailoverType Type { get; set; }
        public FailoverStatus Status { get; set; }
        public DateTime InitiatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string InitiatedBy { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public List<string> StepsCompleted { get; set; } = new();
        public List<string> StepsPending { get; set; } = new();
        public string CurrentStep { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class RecoveryTestDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RecoveryTestType Type { get; set; }
        public DateTime ScheduledDate { get; set; }
        public TimeSpan EstimatedDuration { get; set; }
        public string DrPlanId { get; set; } = string.Empty;
        public List<string> TestScenarios { get; set; } = new();
        public RecoveryTestStatus Status { get; set; }
        public string ResponsiblePerson { get; set; } = string.Empty;
        public bool RequiresDowntime { get; set; }
        public string ImpactAssessment { get; set; } = string.Empty;
    }

    public class RecoveryTestResultDto
    {
        public string TestId { get; set; } = string.Empty;
        public DateTime ExecutionDate { get; set; }
        public TimeSpan ActualDuration { get; set; }
        public bool Successful { get; set; }
        public List<TestScenarioResultDto> ScenarioResults { get; set; } = new();
        public List<string> IssuesIdentified { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public string ExecutedBy { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }

    public class TestScenarioResultDto
    {
        public string Scenario { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public TimeSpan Duration { get; set; }
        public string Result { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class DataIntegrityCheckDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CheckDate { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Passed { get; set; }
        public int TablesChecked { get; set; }
        public int IssuesFound { get; set; }
        public List<DataIntegrityIssueDto> Issues { get; set; } = new();
        public string ChecksumResults { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
    }

    public class DataIntegrityIssueDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TableName { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public bool CanAutoRepair { get; set; }
        public string RepairAction { get; set; } = string.Empty;
        public bool IsRepaired { get; set; }
    }

    public class RtoRpoMetricsDto
    {
        public TimeSpan RecoveryTimeObjective { get; set; }
        public TimeSpan RecoveryPointObjective { get; set; }
        public TimeSpan ActualRecoveryTime { get; set; }
        public TimeSpan ActualRecoveryPoint { get; set; }
        public bool RtoCompliant { get; set; }
        public bool RpoCompliant { get; set; }
        public DateTime LastMeasured { get; set; }
        public string MeasurementContext { get; set; } = string.Empty;
    }

    public class RtoRpoComplianceDto
    {
        public DateTime Date { get; set; }
        public string IncidentId { get; set; } = string.Empty;
        public TimeSpan TargetRto { get; set; }
        public TimeSpan ActualRto { get; set; }
        public TimeSpan TargetRpo { get; set; }
        public TimeSpan ActualRpo { get; set; }
        public bool RtoMet { get; set; }
        public bool RpoMet { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    // Enums
    public enum BackupType
    {
        Full,
        Incremental,
        Differential,
        Log,
        Configuration,
        UserData
    }

    public enum BackupStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled,
        Expired
    }

    public enum DisasterType
    {
        Hardware,
        Software,
        Network,
        Power,
        Security,
        Natural,
        Human,
        DataCorruption
    }

    public enum IncidentType
    {
        System,
        Security,
        Performance,
        Data,
        Network,
        Application,
        Infrastructure
    }

    public enum IncidentSeverity
    {
        Critical,
        High,
        Medium,
        Low
    }

    public enum IncidentStatus
    {
        New,
        Acknowledged,
        Investigating,
        InProgress,
        Resolved,
        Closed
    }

    public enum HealthStatus
    {
        Healthy,
        Warning,
        Critical,
        Unknown
    }

    public enum HealthCheckType
    {
        Database,
        WebService,
        FileSystem,
        Network,
        Memory,
        CPU,
        Custom
    }

    public enum FailoverType
    {
        Automatic,
        Manual,
        Planned,
        Emergency
    }

    public enum FailoverStatus
    {
        NotActive,
        Initiating,
        InProgress,
        Completed,
        Failed,
        RollingBack
    }

    public enum RecoveryTestType
    {
        Full,
        Partial,
        Tabletop,
        Live,
        Simulation
    }

    public enum RecoveryTestStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }
}
