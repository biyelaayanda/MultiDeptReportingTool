using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.DTOs.DepartmentSpecific;
using MultiDeptReportingTool.Services.DepartmentSpecific;
using System.Security.Claims;

namespace MultiDeptReportingTool.Controllers.DepartmentSpecific
{
    /// <summary>
    /// Controller for IT Department specific operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ITController : ControllerBase
    {
        private readonly IDepartmentReportService _departmentReportService;

        public ITController(IDepartmentReportService departmentReportService)
        {
            _departmentReportService = departmentReportService;
        }

        [HttpPost("reports")]
        public async Task<ActionResult<ITReportResponseDto>> CreateITReport([FromBody] CreateITReportDto createDto)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.CreateITReportAsync(createDto, username);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating IT report: {ex.Message}");
            }
        }

        [HttpGet("reports/{id}")]
        public async Task<ActionResult<ITReportResponseDto>> GetITReport(int id)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetITReportAsync(id, username);
                if (result == null)
                    return NotFound("IT report not found or access denied");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving IT report: {ex.Message}");
            }
        }

        [HttpGet("reports")]
        public async Task<ActionResult<List<ITReportResponseDto>>> GetITReports(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetITReportsAsync(username, status, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving IT reports: {ex.Message}");
            }
        }

        [HttpPut("reports/{id}")]
        public async Task<ActionResult<ITReportResponseDto>> UpdateITReport(int id, [FromBody] ITReportDto itData)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.UpdateITReportAsync(id, itData, username);
                if (result == null)
                    return NotFound("IT report not found or access denied");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating IT report: {ex.Message}");
            }
        }

        [HttpGet("analytics")]
        public async Task<ActionResult> GetITAnalytics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetDepartmentAnalyticsAsync("IT", username, fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving IT analytics: {ex.Message}");
            }
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult> GetITDashboard()
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetDepartmentDashboardAsync("IT", username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving IT dashboard: {ex.Message}");
            }
        }
    }
}
