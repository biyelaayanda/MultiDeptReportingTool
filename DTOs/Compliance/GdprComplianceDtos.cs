namespace MultiDeptReportingTool.DTOs.Compliance
{
    public class PersonalDataExportDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime AccountCreated { get; set; }
        public DateTime LastLogin { get; set; }
        public List<UserActivityDto> Activities { get; set; } = new();
        public List<ReportAccessDto> ReportAccess { get; set; } = new();
        public List<ConsentRecordDto> ConsentHistory { get; set; } = new();
        public List<ProcessingRecordDto> ProcessingHistory { get; set; } = new();
        public DateTime ExportDate { get; set; }
        public string ExportFormat { get; set; } = "JSON";
    }

    public class PersonalDataSummaryDto
    {
        public string UserId { get; set; } = string.Empty;
        public int TotalActivities { get; set; }
        public int TotalReports { get; set; }
        public int TotalSessions { get; set; }
        public DateTime DataRetentionExpiry { get; set; }
        public List<string> DataCategories { get; set; } = new();
        public List<string> ProcessingPurposes { get; set; } = new();
        public bool HasActiveConsent { get; set; }
        public DateTime LastConsentUpdate { get; set; }
    }

    public class ConsentRecordDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public ConsentType ConsentType { get; set; }
        public bool Granted { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string LegalBasis { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public string Source { get; set; } = string.Empty; // Web, Mobile, API
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
    }

    public class ProcessingActivityDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataController { get; set; } = string.Empty;
        public string DataProcessor { get; set; } = string.Empty;
        public List<string> DataCategories { get; set; } = new();
        public List<string> DataSubjectCategories { get; set; } = new();
        public string Purpose { get; set; } = string.Empty;
        public string LegalBasis { get; set; } = string.Empty;
        public string Recipients { get; set; } = string.Empty;
        public string ThirdCountryTransfers { get; set; } = string.Empty;
        public TimeSpan RetentionPeriod { get; set; }
        public string SecurityMeasures { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DataBreachDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public BreachType BreachType { get; set; }
        public BreachSeverity Severity { get; set; }
        public DateTime DetectedDate { get; set; }
        public DateTime? ContainedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public int AffectedRecords { get; set; }
        public List<string> AffectedDataTypes { get; set; } = new();
        public List<string> AffectedUsers { get; set; } = new();
        public string RootCause { get; set; } = string.Empty;
        public string ImpactAssessment { get; set; } = string.Empty;
        public string ContainmentMeasures { get; set; } = string.Empty;
        public bool AuthorityNotified { get; set; }
        public DateTime? AuthorityNotificationDate { get; set; }
        public bool DataSubjectsNotified { get; set; }
        public DateTime? DataSubjectNotificationDate { get; set; }
        public string ReportedBy { get; set; } = string.Empty;
        public BreachStatus Status { get; set; }
    }

    public class RetentionPolicyDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public TimeSpan RetentionPeriod { get; set; }
        public string LegalBasis { get; set; } = string.Empty;
        public RetentionAction Action { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class DataRetentionReportDto
    {
        public string DataType { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime DeletionDue { get; set; }
        public TimeSpan DaysUntilDeletion { get; set; }
        public string RetentionPolicy { get; set; } = string.Empty;
        public RetentionStatus Status { get; set; }
    }

    public class PrivacyImpactAssessmentDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ProjectName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataController { get; set; } = string.Empty;
        public List<string> DataTypes { get; set; } = new();
        public List<string> ProcessingPurposes { get; set; } = new();
        public PiaRiskLevel RiskLevel { get; set; }
        public string RiskAssessment { get; set; } = string.Empty;
        public List<string> MitigationMeasures { get; set; } = new();
        public PiaStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Assessor { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class ComplianceReportDto
    {
        public DateTime ReportDate { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public int TotalDataSubjects { get; set; }
        public int TotalProcessingActivities { get; set; }
        public int ConsentGranted { get; set; }
        public int ConsentWithdrawn { get; set; }
        public int DataExportRequests { get; set; }
        public int DataDeletionRequests { get; set; }
        public int DataBreaches { get; set; }
        public int AuthorityNotifications { get; set; }
        public List<string> ComplianceViolations { get; set; } = new();
        public List<RetentionPolicyDto> ActiveRetentionPolicies { get; set; } = new();
        public ComplianceStatus OverallStatus { get; set; }
    }

    // Supporting DTOs
    public class UserActivityDto
    {
        public string ActivityType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Details { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }

    public class ReportAccessDto
    {
        public string ReportId { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public DateTime AccessDate { get; set; }
        public string AccessType { get; set; } = string.Empty; // View, Export, Modify
    }

    public class ProcessingRecordDto
    {
        public string Activity { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string LegalBasis { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    // Enums
    public enum ConsentType
    {
        DataProcessing,
        Marketing,
        Analytics,
        Cookies,
        ThirdPartySharing,
        Profiling
    }

    public enum BreachType
    {
        Confidentiality,
        Integrity,
        Availability,
        Combined
    }

    public enum BreachSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum BreachStatus
    {
        Detected,
        Investigating,
        Contained,
        Resolved,
        Closed
    }

    public enum RetentionAction
    {
        Delete,
        Anonymize,
        Archive,
        Review
    }

    public enum RetentionStatus
    {
        Active,
        PendingDeletion,
        Deleted,
        Anonymized,
        Archived
    }

    public enum PiaRiskLevel
    {
        Low,
        Medium,
        High,
        VeryHigh
    }

    public enum PiaStatus
    {
        Draft,
        InReview,
        Approved,
        Rejected,
        RequiresUpdate
    }

    public enum ComplianceStatus
    {
        Compliant,
        MinorViolations,
        MajorViolations,
        NonCompliant
    }
}
