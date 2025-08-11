using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.Attributes;
using MultiDeptReportingTool.Services;
using MultiDeptReportingTool.Services.Interfaces;

namespace MultiDeptReportingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditTestController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditTestController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet("test-manual-audit")]
        [AllowAnonymous]
        public async Task<IActionResult> TestManualAudit()
        {
            // Manually log an audit event
            await _auditService.LogSecurityEventAsync(
                action: "Test",
                resource: "AuditTest",
                userId: 1,
                username: "TestUser",
                isSuccess: true,
                details: "Manual audit test from controller",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                userAgent: HttpContext.Request.Headers.UserAgent,
                severity: "Info"
            );

            return Ok(new { message = "Manual audit logged successfully" });
        }

        [HttpGet("test-attribute-audit")]
        [AllowAnonymous]
        [AuditAction("Test", "AttributeAudit")]
        public IActionResult TestAttributeAudit()
        {
            return Ok(new { message = "Attribute-based audit test completed" });
        }

        [HttpGet("check-audit-logs")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckAuditLogs()
        {
            var logs = await _auditService.GetSecurityAuditLogsAsync(
                startDate: DateTime.UtcNow.AddHours(-1),
                endDate: DateTime.UtcNow,
                pageNumber: 1,
                pageSize: 10
            );

            return Ok(logs);
        }
    }
}
