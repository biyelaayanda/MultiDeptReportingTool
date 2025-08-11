using MultiDeptReportingTool.DTOs.Compliance;
using MultiDeptReportingTool.Models;

namespace MultiDeptReportingTool.Services
{
    public interface IGdprComplianceService
    {
        // Data Subject Rights
        Task<PersonalDataExportDto> ExportPersonalDataAsync(string userId);
        Task<bool> DeletePersonalDataAsync(string userId, string reason);
        Task<bool> AnonymizePersonalDataAsync(string userId, string reason);
        Task<PersonalDataSummaryDto> GetPersonalDataSummaryAsync(string userId);
        
        // Consent Management
        Task<ConsentRecordDto> RecordConsentAsync(string userId, ConsentType consentType, bool granted, string purpose);
        Task<List<ConsentRecordDto>> GetConsentHistoryAsync(string userId);
        Task<bool> UpdateConsentAsync(string userId, ConsentType consentType, bool granted, string reason);
        Task<bool> IsConsentValidAsync(string userId, ConsentType consentType);
        
        // Data Processing Records
        Task<ProcessingActivityDto> CreateProcessingActivityAsync(ProcessingActivityDto activity);
        Task<List<ProcessingActivityDto>> GetProcessingActivitiesAsync();
        Task<bool> LogDataProcessingAsync(string userId, string activity, string purpose, string legalBasis);
        
        // Breach Management
        Task<DataBreachDto> ReportDataBreachAsync(DataBreachDto breach);
        Task<List<DataBreachDto>> GetDataBreachesAsync(DateTime? from = null, DateTime? to = null);
        Task<bool> NotifyDataSubjectAsync(string userId, string breachId, string details);
        Task<bool> NotifyAuthorityAsync(string breachId, string authorityContact);
        
        // Data Retention
        Task<RetentionPolicyDto> CreateRetentionPolicyAsync(RetentionPolicyDto policy);
        Task<List<RetentionPolicyDto>> GetRetentionPoliciesAsync();
        Task<bool> ApplyRetentionPolicyAsync(string dataType, TimeSpan retentionPeriod);
        Task<List<DataRetentionReportDto>> GetDataDueForDeletionAsync();
        
        // Privacy Impact Assessment
        Task<PrivacyImpactAssessmentDto> CreatePiaAsync(PrivacyImpactAssessmentDto pia);
        Task<List<PrivacyImpactAssessmentDto>> GetPiasAsync();
        Task<bool> UpdatePiaStatusAsync(string piaId, PiaStatus status, string notes);
        
        // Compliance Reporting
        Task<ComplianceReportDto> GenerateComplianceReportAsync(DateTime from, DateTime to);
        Task<bool> ValidateDataProcessingLegalBasisAsync(string activity);
        Task<List<string>> GetComplianceViolationsAsync();
    }
}
