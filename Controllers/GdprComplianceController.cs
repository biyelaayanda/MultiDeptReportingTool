using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MultiDeptReportingTool.DTOs.Compliance;
using MultiDeptReportingTool.Services;
using MultiDeptReportingTool.Services.Interfaces;
using MultiDeptReportingTool.Attributes;
using System.Security.Claims;

namespace MultiDeptReportingTool.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GdprComplianceController : ControllerBase
    {
        private readonly IGdprComplianceService _gdprService;
        private readonly IAuditService _auditService;
        private readonly ILogger<GdprComplianceController> _logger;

        public GdprComplianceController(
            IGdprComplianceService gdprService,
            IAuditService auditService,
            ILogger<GdprComplianceController> logger)
        {
            _gdprService = gdprService;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Export personal data for the current user (GDPR Article 15 - Right of Access)
        /// </summary>
        [HttpGet("personal-data/export")]
        [RequirePermission("DATA_EXPORT")]
        public async Task<ActionResult<PersonalDataExportDto>> ExportPersonalData()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                var exportData = await _gdprService.ExportPersonalDataAsync(userId);
                
                await _auditService.LogSecurityEventAsync("PERSONAL_DATA_EXPORT", "PersonalData", 
                    int.Parse(userId), username: User.Identity?.Name, isSuccess: true, 
                    details: "User exported their personal data");

                return Ok(exportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting personal data");
                return StatusCode(500, "An error occurred while exporting personal data");
            }
        }

        /// <summary>
        /// Export personal data for a specific user (Admin only)
        /// </summary>
        [HttpGet("personal-data/export/{userId}")]
        [RequirePermission("ADMIN_DATA_EXPORT")]
        public async Task<ActionResult<PersonalDataExportDto>> ExportPersonalDataForUser(string userId)
        {
            try
            {
                var requestingUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var exportData = await _gdprService.ExportPersonalDataAsync(userId);
                
                var requestingUserIdInt = int.Parse(requestingUserId ?? "0");
                await _auditService.LogSecurityEventAsync("ADMIN_DATA_EXPORT", "PersonalData", 
                    requestingUserIdInt, username: User.Identity?.Name, isSuccess: true, 
                    details: $"Admin exported personal data for user {userId}");

                return Ok(exportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting personal data for user {UserId}", userId);
                return StatusCode(500, "An error occurred while exporting personal data");
            }
        }

        /// <summary>
        /// Get personal data summary for current user
        /// </summary>
        [HttpGet("personal-data/summary")]
        public async Task<ActionResult<PersonalDataSummaryDto>> GetPersonalDataSummary()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                var summary = await _gdprService.GetPersonalDataSummaryAsync(userId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personal data summary");
                return StatusCode(500, "An error occurred while retrieving personal data summary");
            }
        }

        /// <summary>
        /// Request deletion of personal data (GDPR Article 17 - Right to Erasure)
        /// </summary>
        [HttpDelete("personal-data")]
        public async Task<ActionResult> RequestDataDeletion([FromBody] DataDeletionRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                var result = await _gdprService.DeletePersonalDataAsync(userId, request.Reason);
                
                if (result)
                {
                    var userIdInt = int.Parse(userId);
                    await _auditService.LogSecurityEventAsync("DATA_DELETION_REQUEST", "PersonalData", 
                        userIdInt, username: User.Identity?.Name, isSuccess: true, 
                        details: $"User requested data deletion. Reason: {request.Reason}");
                    return Ok(new { message = "Data deletion request processed successfully" });
                }
                
                return BadRequest("Failed to process data deletion request");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing data deletion request");
                return StatusCode(500, "An error occurred while processing the deletion request");
            }
        }

        /// <summary>
        /// Record user consent (GDPR Article 7 - Conditions for consent)
        /// </summary>
        [HttpPost("consent")]
        public async Task<ActionResult<ConsentRecordDto>> RecordConsent([FromBody] ConsentRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                var consent = await _gdprService.RecordConsentAsync(userId, request.ConsentType, 
                    request.Granted, request.Purpose);
                
                var userIdInt = int.Parse(userId);
                await _auditService.LogSecurityEventAsync("CONSENT_RECORDED", "ConsentRecord", 
                    userIdInt, username: User.Identity?.Name, isSuccess: true, 
                    details: $"Consent {(request.Granted ? "granted" : "withdrawn")} for {request.ConsentType}");

                return Ok(consent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording consent");
                return StatusCode(500, "An error occurred while recording consent");
            }
        }

        /// <summary>
        /// Update existing consent
        /// </summary>
        [HttpPut("consent/{consentType}")]
        public async Task<ActionResult> UpdateConsent(ConsentType consentType, [FromBody] ConsentUpdateDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                var result = await _gdprService.UpdateConsentAsync(userId, consentType, 
                    request.Granted, request.Reason);
                
                if (result)
                {
                    var userIdInt = int.Parse(userId);
                    await _auditService.LogSecurityEventAsync("CONSENT_UPDATED", "ConsentRecord", 
                        userIdInt, username: User.Identity?.Name, isSuccess: true, 
                        details: $"Consent updated for {consentType}: {(request.Granted ? "granted" : "withdrawn")}");
                    return Ok(new { message = "Consent updated successfully" });
                }
                
                return BadRequest("Failed to update consent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating consent");
                return StatusCode(500, "An error occurred while updating consent");
            }
        }

        /// <summary>
        /// Get consent history for current user
        /// </summary>
        [HttpGet("consent/history")]
        public async Task<ActionResult<List<ConsentRecordDto>>> GetConsentHistory()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                var history = await _gdprService.GetConsentHistoryAsync(userId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting consent history");
                return StatusCode(500, "An error occurred while retrieving consent history");
            }
        }

        /// <summary>
        /// Check if specific consent is valid
        /// </summary>
        [HttpGet("consent/{consentType}/valid")]
        public async Task<ActionResult<bool>> IsConsentValid(ConsentType consentType)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                var isValid = await _gdprService.IsConsentValidAsync(userId, consentType);
                return Ok(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking consent validity");
                return StatusCode(500, "An error occurred while checking consent validity");
            }
        }

        /// <summary>
        /// Create processing activity (Admin only - GDPR Article 30)
        /// </summary>
        [HttpPost("processing-activities")]
        [RequirePermission("GDPR_ADMIN")]
        public async Task<ActionResult<ProcessingActivityDto>> CreateProcessingActivity([FromBody] ProcessingActivityDto activity)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var createdActivity = await _gdprService.CreateProcessingActivityAsync(activity);
                
                var userIdInt = int.Parse(userId ?? "0");
                await _auditService.LogSecurityEventAsync("PROCESSING_ACTIVITY_CREATED", "ProcessingActivity", 
                    userIdInt, username: User.Identity?.Name, isSuccess: true, 
                    details: $"Processing activity created: {activity.Name}");

                return Ok(createdActivity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating processing activity");
                return StatusCode(500, "An error occurred while creating processing activity");
            }
        }

        /// <summary>
        /// Get all processing activities (Admin only)
        /// </summary>
        [HttpGet("processing-activities")]
        [RequirePermission("GDPR_ADMIN")]
        public async Task<ActionResult<List<ProcessingActivityDto>>> GetProcessingActivities()
        {
            try
            {
                var activities = await _gdprService.GetProcessingActivitiesAsync();
                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting processing activities");
                return StatusCode(500, "An error occurred while retrieving processing activities");
            }
        }

        /// <summary>
        /// Generate GDPR compliance report (Admin only)
        /// </summary>
        [HttpGet("compliance-report")]
        [RequirePermission("GDPR_ADMIN")]
        public async Task<ActionResult<ComplianceReportDto>> GenerateComplianceReport(
            [FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var report = await _gdprService.GenerateComplianceReportAsync(from, to);
                
                var userIdInt = int.Parse(userId ?? "0");
                await _auditService.LogSecurityEventAsync("COMPLIANCE_REPORT_GENERATED", "ComplianceReport", 
                    userIdInt, username: User.Identity?.Name, isSuccess: true, 
                    details: $"GDPR compliance report generated for period {from:yyyy-MM-dd} to {to:yyyy-MM-dd}");

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating compliance report");
                return StatusCode(500, "An error occurred while generating compliance report");
            }
        }

        /// <summary>
        /// Get compliance violations (Admin only)
        /// </summary>
        [HttpGet("violations")]
        [RequirePermission("GDPR_ADMIN")]
        public async Task<ActionResult<List<string>>> GetComplianceViolations()
        {
            try
            {
                var violations = await _gdprService.GetComplianceViolationsAsync();
                return Ok(violations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compliance violations");
                return StatusCode(500, "An error occurred while retrieving compliance violations");
            }
        }
    }

    // Supporting DTOs for API requests
    public class DataDeletionRequestDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class ConsentRequestDto
    {
        public ConsentType ConsentType { get; set; }
        public bool Granted { get; set; }
        public string Purpose { get; set; } = string.Empty;
    }

    public class ConsentUpdateDto
    {
        public bool Granted { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
