using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MultiDeptReportingTool.DTOs.Compliance;

namespace MultiDeptReportingTool.Models
{
    public class ConsentRecord
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public ConsentType ConsentType { get; set; }
        
        [Required]
        public bool Granted { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Purpose { get; set; } = string.Empty;
        
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [StringLength(200)]
        public string LegalBasis { get; set; } = string.Empty;
        
        public DateTime? ExpiryDate { get; set; }
        
        [StringLength(50)]
        public string Source { get; set; } = string.Empty;
        
        [StringLength(45)]
        public string IpAddress { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string UserAgent { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual Users User { get; set; } = null!;
    }

    public class ProcessingActivity
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string DataController { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string DataProcessor { get; set; } = string.Empty;
        
        [Required]
        public string DataCategories { get; set; } = string.Empty; // JSON array
        
        [Required]
        public string DataSubjectCategories { get; set; } = string.Empty; // JSON array
        
        [Required]
        [StringLength(500)]
        public string Purpose { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string LegalBasis { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Recipients { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string ThirdCountryTransfers { get; set; } = string.Empty;
        
        [Required]
        public long RetentionPeriodDays { get; set; }
        
        [StringLength(1000)]
        public string SecurityMeasures { get; set; } = string.Empty;
        
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class DataBreach
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public BreachType BreachType { get; set; }
        
        [Required]
        public BreachSeverity Severity { get; set; }
        
        [Required]
        public DateTime DetectedDate { get; set; }
        
        public DateTime? ContainedDate { get; set; }
        
        public DateTime? ResolvedDate { get; set; }
        
        [Required]
        public int AffectedRecords { get; set; }
        
        [Required]
        public string AffectedDataTypes { get; set; } = string.Empty; // JSON array
        
        [Required]
        public string AffectedUsers { get; set; } = string.Empty; // JSON array
        
        [StringLength(1000)]
        public string RootCause { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string ImpactAssessment { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string ContainmentMeasures { get; set; } = string.Empty;
        
        public bool AuthorityNotified { get; set; }
        
        public DateTime? AuthorityNotificationDate { get; set; }
        
        public bool DataSubjectsNotified { get; set; }
        
        public DateTime? DataSubjectNotificationDate { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ReportedBy { get; set; } = string.Empty;
        
        [Required]
        public BreachStatus Status { get; set; }
        
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class RetentionPolicy
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string DataType { get; set; } = string.Empty;
        
        [Required]
        public long RetentionPeriodDays { get; set; }
        
        [Required]
        [StringLength(200)]
        public string LegalBasis { get; set; } = string.Empty;
        
        [Required]
        public RetentionAction Action { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
    }

    public class PrivacyImpactAssessment
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(200)]
        public string ProjectName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string DataController { get; set; } = string.Empty;
        
        [Required]
        public string DataTypes { get; set; } = string.Empty; // JSON array
        
        [Required]
        public string ProcessingPurposes { get; set; } = string.Empty; // JSON array
        
        [Required]
        public PiaRiskLevel RiskLevel { get; set; }
        
        [Required]
        [StringLength(2000)]
        public string RiskAssessment { get; set; } = string.Empty;
        
        [Required]
        public string MitigationMeasures { get; set; } = string.Empty; // JSON array
        
        [Required]
        public PiaStatus Status { get; set; }
        
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? CompletedDate { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Assessor { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string Notes { get; set; } = string.Empty;
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class DataProcessingLog
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Activity { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string Purpose { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string LegalBasis { get; set; } = string.Empty;
        
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [StringLength(100)]
        public string DataType { get; set; } = string.Empty;
        
        [StringLength(45)]
        public string IpAddress { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string UserAgent { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string Details { get; set; } = string.Empty;
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual Users User { get; set; } = null!;
    }

    public class DataExportRequest
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public ExportStatus Status { get; set; }
        
        [StringLength(200)]
        public string RequestReason { get; set; } = string.Empty;
        
        public DateTime? CompletedDate { get; set; }
        
        [StringLength(500)]
        public string ExportFilePath { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string ExportFormat { get; set; } = "JSON";
        
        public long? FileSizeBytes { get; set; }
        
        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual Users User { get; set; } = null!;
    }

    public class DataDeletionRequest
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public DeletionStatus Status { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
        
        public DateTime? CompletedDate { get; set; }
        
        [StringLength(1000)]
        public string DeletedDataSummary { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string ProcessedBy { get; set; } = string.Empty;
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual Users User { get; set; } = null!;
    }

    // Additional enums
    public enum ExportStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Expired
    }

    public enum DeletionStatus
    {
        Pending,
        Approved,
        Processing,
        Completed,
        Rejected,
        Failed
    }
}
