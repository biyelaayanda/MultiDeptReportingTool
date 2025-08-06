using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.DTOs.DepartmentSpecific;
using MultiDeptReportingTool.Services.DepartmentSpecific;
using System.Security.Claims;

namespace MultiDeptReportingTool.Controllers.DepartmentSpecific
{
    /// <summary>
    /// Controller for Operations Department specific operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OperationsController : ControllerBase
    {
        private readonly IDepartmentReportService _departmentReportService;

        public OperationsController(IDepartmentReportService departmentReportService)
        {
            _departmentReportService = departmentReportService;
        }

        [HttpPost("reports")]
        public async Task<ActionResult<OperationsReportResponseDto>> CreateOperationsReport([FromBody] CreateOperationsReportDto createDto)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.CreateOperationsReportAsync(createDto, username);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating Operations report: {ex.Message}");
            }
        }

        [HttpGet("reports/{id}")]
        public async Task<ActionResult<OperationsReportResponseDto>> GetOperationsReport(int id)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetOperationsReportAsync(id, username);
                if (result == null)
                    return NotFound("Operations report not found or access denied");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving Operations report: {ex.Message}");
            }
        }

        [HttpGet("reports")]
        public async Task<ActionResult<List<OperationsReportResponseDto>>> GetOperationsReports(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetOperationsReportsAsync(username, status, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving Operations reports: {ex.Message}");
            }
        }

        [HttpPut("reports/{id}")]
        public async Task<ActionResult<OperationsReportResponseDto>> UpdateOperationsReport(int id, [FromBody] OperationsReportDto operationsData)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.UpdateOperationsReportAsync(id, operationsData, username);
                if (result == null)
                    return NotFound("Operations report not found or access denied");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating Operations report: {ex.Message}");
            }
        }

        [HttpGet("analytics")]
        public async Task<ActionResult> GetOperationsAnalytics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetDepartmentAnalyticsAsync("Operations", username, fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving Operations analytics: {ex.Message}");
            }
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult> GetOperationsDashboard()
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetDepartmentDashboardAsync("Operations", username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving Operations dashboard: {ex.Message}");
            }
        }
    }
}
