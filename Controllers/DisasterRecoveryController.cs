using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.Attributes;
using MultiDeptReportingTool.DTOs.DisasterRecovery;
using MultiDeptReportingTool.Services.Interfaces;

namespace MultiDeptReportingTool.Controllers
{
    [Authorize]
    [RequirePermission("DISASTER_RECOVERY")]
    [Route("api/[controller]")]
    [ApiController]
    public class DisasterRecoveryController : ControllerBase
    {
        private readonly IDisasterRecoveryService _disasterRecoveryService;
        private readonly IAuditService _auditService;
        private readonly ILogger<DisasterRecoveryController> _logger;

        public DisasterRecoveryController(
            IDisasterRecoveryService disasterRecoveryService,
            IAuditService auditService,
            ILogger<DisasterRecoveryController> logger)
        {
            _disasterRecoveryService = disasterRecoveryService;
            _auditService = auditService;
            _logger = logger;
        }

        // Backup Management Endpoints
        [HttpPost("backups")]
        [RequirePermission("BACKUP_MANAGE")]
        public async Task<ActionResult<BackupJobDto>> CreateBackupJob([FromBody] CreateBackupJobRequest request)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? "anonymous";
                var backup = await _disasterRecoveryService.CreateBackupJobAsync(
                    request.Name, request.Type, request.Schedule, request.DataSources);

                await _auditService.LogSecurityEventAsync("BACKUP_JOB_CREATED", "BackupJob", 
                    backup.Id, $"Backup job created: {request.Name}", userId);

                return Ok(backup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup job");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("backups")]
        [RequirePermission("BACKUP_VIEW")]
        public async Task<ActionResult<List<BackupJobDto>>> GetBackupJobs()
        {
            try
            {
                var backups = await _disasterRecoveryService.GetBackupJobsAsync();
                return Ok(backups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving backup jobs");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("backups/{jobId}/run")]
        [RequirePermission("BACKUP_EXECUTE")]
        public async Task<ActionResult> RunBackupJob(string jobId)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? "anonymous";
                var success = await _disasterRecoveryService.RunBackupJobAsync(jobId);

                if (success)
                {
                    await _auditService.LogSecurityEventAsync("BACKUP_EXECUTED", "BackupJob", 
                        jobId, "Backup job executed manually", userId);
                    return Ok(new { message = "Backup job started successfully" });
                }

                return BadRequest("Failed to start backup job");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running backup job {JobId}", jobId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("backups/{jobId}/status")]
        [RequirePermission("BACKUP_VIEW")]
        public async Task<ActionResult<BackupStatusDto>> GetBackupStatus(string jobId)
        {
            try
            {
                var status = await _disasterRecoveryService.GetBackupStatusAsync(jobId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting backup status for job {JobId}", jobId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("restore")]
        [RequirePermission("RESTORE_EXECUTE")]
        public async Task<ActionResult> RestoreFromBackup([FromBody] RestoreRequest request)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? "anonymous";
                var success = await _disasterRecoveryService.RestoreFromBackupAsync(
                    request.BackupId, request.TargetLocation);

                if (success)
                {
                    await _auditService.LogSecurityEventAsync("SYSTEM_RESTORED", "DisasterRecovery", 
                        request.BackupId, $"System restored to {request.TargetLocation}", userId);
                    return Ok(new { message = "Restore initiated successfully" });
                }

                return BadRequest("Failed to initiate restore");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring from backup");
                return StatusCode(500, "Internal server error");
            }
        }

        // Incident Response Endpoints
        [HttpPost("incidents")]
        [RequirePermission("INCIDENT_MANAGE")]
        public async Task<ActionResult<IncidentDto>> CreateIncident([FromBody] IncidentDto incident)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? "anonymous";
                var createdIncident = await _disasterRecoveryService.CreateIncidentAsync(incident);

                await _auditService.LogSecurityEventAsync("INCIDENT_CREATED", "Incident", 
                    createdIncident.Id, $"Incident created: {incident.Title}", userId);

                return Ok(createdIncident);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating incident");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("incidents")]
        [RequirePermission("INCIDENT_VIEW")]
        public async Task<ActionResult<List<IncidentDto>>> GetIncidents([FromQuery] IncidentStatus? status = null)
        {
            try
            {
                var incidents = await _disasterRecoveryService.GetIncidentsAsync(status);
                return Ok(incidents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incidents");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("incidents/{incidentId}/status")]
        [RequirePermission("INCIDENT_MANAGE")]
        public async Task<ActionResult> UpdateIncidentStatus(string incidentId, [FromBody] UpdateIncidentStatusRequest request)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? "anonymous";
                var success = await _disasterRecoveryService.UpdateIncidentStatusAsync(
                    incidentId, request.Status, request.Notes);

                if (success)
                {
                    await _auditService.LogSecurityEventAsync("INCIDENT_UPDATED", "Incident", 
                        incidentId, $"Status updated to {request.Status}", userId);
                    return Ok(new { message = "Incident status updated successfully" });
                }

                return BadRequest("Failed to update incident status");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating incident {IncidentId}", incidentId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("incidents/{incidentId}/response-plan/{planId}")]
        [RequirePermission("RESPONSE_EXECUTE")]
        public async Task<ActionResult> ExecuteResponsePlan(string incidentId, string planId)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? "anonymous";
                var success = await _disasterRecoveryService.ExecuteResponsePlanAsync(planId, incidentId);

                if (success)
                {
                    await _auditService.LogSecurityEventAsync("RESPONSE_PLAN_EXECUTED", "ResponsePlan", 
                        planId, $"Executed for incident {incidentId}", userId);
                    return Ok(new { message = "Response plan executed successfully" });
                }

                return BadRequest("Failed to execute response plan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing response plan");
                return StatusCode(500, "Internal server error");
            }
        }

        // System Health Endpoints
        [HttpGet("health")]
        [RequirePermission("HEALTH_VIEW")]
        public async Task<ActionResult<SystemHealthDto>> GetSystemHealth()
        {
            try
            {
                var health = await _disasterRecoveryService.GetSystemHealthAsync();
                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("health/check/{serviceName}")]
        [RequirePermission("HEALTH_CHECK")]
        public async Task<ActionResult> RunHealthCheck(string serviceName)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? "anonymous";
                var success = await _disasterRecoveryService.RunHealthCheckAsync(serviceName);

                await _auditService.LogSecurityEventAsync("HEALTH_CHECK_EXECUTED", "HealthCheck", 
                    serviceName, $"Health check result: {(success ? "Success" : "Failed")}", userId);

                return Ok(new { serviceName, success, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running health check for {Service}", serviceName);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("metrics")]
        [RequirePermission("METRICS_VIEW")]
        public async Task<ActionResult<List<PerformanceMetricDto>>> GetPerformanceMetrics(
            [FromQuery] DateTime? from = null, 
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
                var toDate = to ?? DateTime.UtcNow;
                
                var metrics = await _disasterRecoveryService.GetPerformanceMetricsAsync(fromDate, toDate);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics");
                return StatusCode(500, "Internal server error");
            }
        }

        // Failover Management Endpoints
        [HttpPost("failover")]
        [RequirePermission("FAILOVER_EXECUTE")]
        public async Task<ActionResult> InitiateFailover([FromBody] FailoverRequest request)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? "anonymous";
                var success = await _disasterRecoveryService.InitiateFailoverAsync(
                    request.PrimarySystemId, request.BackupSystemId);

                if (success)
                {
                    await _auditService.LogSecurityEventAsync("FAILOVER_INITIATED", "Failover", 
                        request.PrimarySystemId, $"Failover to {request.BackupSystemId}", userId);
                    return Ok(new { message = "Failover initiated successfully" });
                }

                return BadRequest("Failed to initiate failover");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating failover");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("failover/status")]
        [RequirePermission("FAILOVER_VIEW")]
        public async Task<ActionResult<FailoverStatusDto>> GetFailoverStatus()
        {
            try
            {
                var status = await _disasterRecoveryService.GetFailoverStatusAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting failover status");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("failover/test/{targetSystemId}")]
        [RequirePermission("FAILOVER_TEST")]
        public async Task<ActionResult> TestFailover(string targetSystemId)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? "anonymous";
                var success = await _disasterRecoveryService.TestFailoverAsync(targetSystemId);

                await _auditService.LogSecurityEventAsync("FAILOVER_TESTED", "FailoverTest", 
                    targetSystemId, $"Test result: {(success ? "Success" : "Failed")}", userId);

                return Ok(new { targetSystemId, success, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing failover to {Target}", targetSystemId);
                return StatusCode(500, "Internal server error");
            }
        }

        // Recovery Testing Endpoints
        [HttpPost("recovery-tests")]
        [RequirePermission("RECOVERY_TEST_MANAGE")]
        public async Task<ActionResult<RecoveryTestDto>> CreateRecoveryTest([FromBody] RecoveryTestDto test)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? "anonymous";
                var createdTest = await _disasterRecoveryService.CreateRecoveryTestAsync(test);

                await _auditService.LogSecurityEventAsync("RECOVERY_TEST_CREATED", "RecoveryTest", 
                    createdTest.Id, $"Recovery test created: {test.Name}", userId);

                return Ok(createdTest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating recovery test");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("recovery-tests/{testId}/execute")]
        [RequirePermission("RECOVERY_TEST_EXECUTE")]
        public async Task<ActionResult> ExecuteRecoveryTest(string testId)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? "anonymous";
                var success = await _disasterRecoveryService.ExecuteRecoveryTestAsync(testId);

                await _auditService.LogSecurityEventAsync("RECOVERY_TEST_EXECUTED", "RecoveryTest", 
                    testId, $"Test execution result: {(success ? "Success" : "Failed")}", userId);

                return Ok(new { testId, success, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing recovery test {TestId}", testId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("recovery-tests")]
        [RequirePermission("RECOVERY_TEST_VIEW")]
        public async Task<ActionResult<List<RecoveryTestDto>>> GetRecoveryTests()
        {
            try
            {
                var tests = await _disasterRecoveryService.GetRecoveryTestsAsync();
                return Ok(tests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recovery tests");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("recovery-metrics")]
        [RequirePermission("RECOVERY_METRICS_VIEW")]
        public async Task<ActionResult<RecoveryMetricsDto>> GetRecoveryMetrics()
        {
            try
            {
                var metrics = await _disasterRecoveryService.GetRecoveryMetricsAsync();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recovery metrics");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    // Request/Response DTOs for the controller
    public class CreateBackupJobRequest
    {
        public string Name { get; set; } = string.Empty;
        public BackupType Type { get; set; }
        public string Schedule { get; set; } = string.Empty;
        public List<string> DataSources { get; set; } = new();
    }

    public class RestoreRequest
    {
        public string BackupId { get; set; } = string.Empty;
        public string TargetLocation { get; set; } = string.Empty;
    }

    public class UpdateIncidentStatusRequest
    {
        public IncidentStatus Status { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class FailoverRequest
    {
        public string PrimarySystemId { get; set; } = string.Empty;
        public string BackupSystemId { get; set; } = string.Empty;
    }
}
