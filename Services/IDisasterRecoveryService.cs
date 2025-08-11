using MultiDeptReportingTool.DTOs.DisasterRecovery;

namespace MultiDeptReportingTool.Services
{
    public interface IDisasterRecoveryService
    {
        // Backup Management
        Task<BackupDto> CreateBackupAsync(BackupType backupType, string description = "");
        Task<List<BackupDto>> GetBackupsAsync(BackupType? type = null, DateTime? from = null, DateTime? to = null);
        Task<bool> RestoreFromBackupAsync(string backupId, RestoreOptions options);
        Task<bool> DeleteBackupAsync(string backupId);
        Task<BackupValidationDto> ValidateBackupAsync(string backupId);
        
        // Automated Backup Scheduling
        Task<BackupScheduleDto> CreateBackupScheduleAsync(BackupScheduleDto schedule);
        Task<List<BackupScheduleDto>> GetBackupSchedulesAsync();
        Task<bool> UpdateBackupScheduleAsync(string scheduleId, BackupScheduleDto schedule);
        Task<bool> DeleteBackupScheduleAsync(string scheduleId);
        Task ExecuteScheduledBackupsAsync();
        
        // Disaster Recovery Planning
        Task<DisasterRecoveryPlanDto> CreateRecoveryPlanAsync(DisasterRecoveryPlanDto plan);
        Task<List<DisasterRecoveryPlanDto>> GetRecoveryPlansAsync();
        Task<bool> UpdateRecoveryPlanAsync(string planId, DisasterRecoveryPlanDto plan);
        Task<bool> ActivateRecoveryPlanAsync(string planId, string incidentId);
        
        // Incident Management
        Task<IncidentDto> ReportIncidentAsync(IncidentDto incident);
        Task<List<IncidentDto>> GetIncidentsAsync(IncidentSeverity? severity = null, IncidentStatus? status = null);
        Task<bool> UpdateIncidentStatusAsync(string incidentId, IncidentStatus status, string notes);
        Task<IncidentResponseDto> GetIncidentResponseAsync(string incidentId);
        
        // System Health Monitoring
        Task<SystemHealthDto> GetSystemHealthAsync();
        Task<List<SystemHealthDto>> GetSystemHealthHistoryAsync(TimeSpan period);
        Task<bool> RunHealthCheckAsync();
        Task<List<HealthCheckDto>> GetHealthChecksAsync();
        
        // Failover Management
        Task<bool> InitiateFailoverAsync(FailoverType failoverType, string reason);
        Task<FailoverStatusDto> GetFailoverStatusAsync();
        Task<bool> CompleteFailoverAsync(string failoverId);
        Task<bool> RollbackFailoverAsync(string failoverId, string reason);
        
        // Recovery Testing
        Task<RecoveryTestDto> ScheduleRecoveryTestAsync(RecoveryTestDto test);
        Task<List<RecoveryTestDto>> GetRecoveryTestsAsync();
        Task<RecoveryTestResultDto> ExecuteRecoveryTestAsync(string testId);
        Task<bool> ValidateRecoveryProceduresAsync();
        
        // Data Integrity
        Task<DataIntegrityCheckDto> PerformDataIntegrityCheckAsync();
        Task<List<DataIntegrityCheckDto>> GetDataIntegrityHistoryAsync(DateTime from, DateTime to);
        Task<bool> RepairDataIntegrityIssuesAsync(List<string> issueIds);
        
        // Recovery Time and Point Objectives
        Task<RtoRpoMetricsDto> CalculateRtoRpoMetricsAsync();
        Task<bool> UpdateRtoRpoTargetsAsync(TimeSpan rtoTarget, TimeSpan rpoTarget);
        Task<List<RtoRpoComplianceDto>> GetRtoRpoComplianceReportAsync(DateTime from, DateTime to);
    }
}
