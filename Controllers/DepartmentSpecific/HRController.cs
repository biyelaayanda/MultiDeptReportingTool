using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.DTOs.DepartmentSpecific;
using MultiDeptReportingTool.Services.DepartmentSpecific;
using System.Security.Claims;

namespace MultiDeptReportingTool.Controllers.DepartmentSpecific
{
    /// <summary>
    /// Controller for HR Department specific operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HRController : ControllerBase
    {
        private readonly IDepartmentReportService _departmentReportService;

        public HRController(IDepartmentReportService departmentReportService)
        {
            _departmentReportService = departmentReportService;
        }

        [HttpPost("reports")]
        public async Task<ActionResult<HRReportResponseDto>> CreateHRReport([FromBody] CreateHRReportDto createDto)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.CreateHRReportAsync(createDto, username);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating HR report: {ex.Message}");
            }
        }

        [HttpGet("reports/{id}")]
        public async Task<ActionResult<HRReportResponseDto>> GetHRReport(int id)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetHRReportAsync(id, username);
                if (result == null)
                    return NotFound("HR report not found or access denied");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving HR report: {ex.Message}");
            }
        }

        [HttpGet("reports")]
        public async Task<ActionResult<List<HRReportResponseDto>>> GetHRReports(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetHRReportsAsync(username, status, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving HR reports: {ex.Message}");
            }
        }

        [HttpPut("reports/{id}")]
        public async Task<ActionResult<HRReportResponseDto>> UpdateHRReport(int id, [FromBody] HRReportDto hrData)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.UpdateHRReportAsync(id, hrData, username);
                if (result == null)
                    return NotFound("HR report not found or access denied");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating HR report: {ex.Message}");
            }
        }

        [HttpGet("analytics")]
        public async Task<ActionResult> GetHRAnalytics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetDepartmentAnalyticsAsync("Human Resources", username, fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving HR analytics: {ex.Message}");
            }
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult> GetHRDashboard()
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetDepartmentDashboardAsync("Human Resources", username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving HR dashboard: {ex.Message}");
            }
        }
    }
}
