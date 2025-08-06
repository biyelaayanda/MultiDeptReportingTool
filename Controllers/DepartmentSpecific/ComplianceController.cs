using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.DTOs.DepartmentSpecific;
using MultiDeptReportingTool.Services.DepartmentSpecific;
using System.Security.Claims;

namespace MultiDeptReportingTool.Controllers.DepartmentSpecific
{
    /// <summary>
    /// Controller for Compliance Department specific operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComplianceController : ControllerBase
    {
        private readonly IDepartmentReportService _departmentReportService;

        public ComplianceController(IDepartmentReportService departmentReportService)
        {
            _departmentReportService = departmentReportService;
        }

        [HttpPost("reports")]
        public async Task<ActionResult<ComplianceReportResponseDto>> CreateComplianceReport([FromBody] CreateComplianceReportDto createDto)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.CreateComplianceReportAsync(createDto, username);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating Compliance report: {ex.Message}");
            }
        }

        [HttpGet("reports/{id}")]
        public async Task<ActionResult<ComplianceReportResponseDto>> GetComplianceReport(int id)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetComplianceReportAsync(id, username);
                if (result == null)
                    return NotFound("Compliance report not found or access denied");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving Compliance report: {ex.Message}");
            }
        }

        [HttpGet("reports")]
        public async Task<ActionResult<List<ComplianceReportResponseDto>>> GetComplianceReports(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetComplianceReportsAsync(username, status, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving Compliance reports: {ex.Message}");
            }
        }

        [HttpPut("reports/{id}")]
        public async Task<ActionResult<ComplianceReportResponseDto>> UpdateComplianceReport(int id, [FromBody] ComplianceReportDto complianceData)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.UpdateComplianceReportAsync(id, complianceData, username);
                if (result == null)
                    return NotFound("Compliance report not found or access denied");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating Compliance report: {ex.Message}");
            }
        }

        [HttpGet("analytics")]
        public async Task<ActionResult> GetComplianceAnalytics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetDepartmentAnalyticsAsync("Compliance", username, fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving Compliance analytics: {ex.Message}");
            }
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult> GetComplianceDashboard()
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetDepartmentDashboardAsync("Compliance", username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving Compliance dashboard: {ex.Message}");
            }
        }
    }
}
