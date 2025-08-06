using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.DTOs.DepartmentSpecific;
using MultiDeptReportingTool.Services.DepartmentSpecific;
using System.Security.Claims;

namespace MultiDeptReportingTool.Controllers.DepartmentSpecific
{
    /// <summary>
    /// Controller for Finance Department specific operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FinanceController : ControllerBase
    {
        private readonly IDepartmentReportService _departmentReportService;

        public FinanceController(IDepartmentReportService departmentReportService)
        {
            _departmentReportService = departmentReportService;
        }

        /// <summary>
        /// Create a new finance report
        /// </summary>
        [HttpPost("reports")]
        public async Task<ActionResult<FinanceReportResponseDto>> CreateFinanceReport([FromBody] CreateFinanceReportDto createDto)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.CreateFinanceReportAsync(createDto, username);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating finance report: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a specific finance report by ID
        /// </summary>
        [HttpGet("reports/{id}")]
        public async Task<ActionResult<FinanceReportResponseDto>> GetFinanceReport(int id)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetFinanceReportAsync(id, username);
                if (result == null)
                    return NotFound("Finance report not found or access denied");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving finance report: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all finance reports with optional filtering
        /// </summary>
        [HttpGet("reports")]
        public async Task<ActionResult<List<FinanceReportResponseDto>>> GetFinanceReports(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetFinanceReportsAsync(username, status, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving finance reports: {ex.Message}");
            }
        }

        /// <summary>
        /// Update a finance report
        /// </summary>
        [HttpPut("reports/{id}")]
        public async Task<ActionResult<FinanceReportResponseDto>> UpdateFinanceReport(int id, [FromBody] FinanceReportDto financeData)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.UpdateFinanceReportAsync(id, financeData, username);
                if (result == null)
                    return NotFound("Finance report not found or access denied");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating finance report: {ex.Message}");
            }
        }

        /// <summary>
        /// Submit a finance report for approval
        /// </summary>
        [HttpPost("reports/{id}/submit")]
        public async Task<ActionResult> SubmitFinanceReport(int id)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.SubmitDepartmentReportAsync(id, username);
                if (!result)
                    return BadRequest("Unable to submit report. Check if report exists and is in draft status.");

                return Ok(new { message = "Finance report submitted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error submitting finance report: {ex.Message}");
            }
        }

        /// <summary>
        /// Approve a finance report
        /// </summary>
        [HttpPost("reports/{id}/approve")]
        [Authorize(Roles = "Admin,Executive,DepartmentLead")]
        public async Task<ActionResult> ApproveFinanceReport(int id, [FromBody] string? comments = null)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.ApproveDepartmentReportAsync(id, username, comments);
                if (!result)
                    return BadRequest("Unable to approve report. Check if report exists and is pending approval.");

                return Ok(new { message = "Finance report approved successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error approving finance report: {ex.Message}");
            }
        }

        /// <summary>
        /// Reject a finance report
        /// </summary>
        [HttpPost("reports/{id}/reject")]
        [Authorize(Roles = "Admin,Executive,DepartmentLead")]
        public async Task<ActionResult> RejectFinanceReport(int id, [FromBody] string comments)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                if (string.IsNullOrEmpty(comments))
                    return BadRequest("Comments are required when rejecting a report");

                var result = await _departmentReportService.RejectDepartmentReportAsync(id, username, comments);
                if (!result)
                    return BadRequest("Unable to reject report. Check if report exists and is pending approval.");

                return Ok(new { message = "Finance report rejected successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error rejecting finance report: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a finance report
        /// </summary>
        [HttpDelete("reports/{id}")]
        public async Task<ActionResult> DeleteFinanceReport(int id)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.DeleteDepartmentReportAsync(id, username);
                if (!result)
                    return BadRequest("Unable to delete report. Only draft reports can be deleted by their creators.");

                return Ok(new { message = "Finance report deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error deleting finance report: {ex.Message}");
            }
        }

        /// <summary>
        /// Get finance department analytics
        /// </summary>
        [HttpGet("analytics")]
        public async Task<ActionResult> GetFinanceAnalytics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetDepartmentAnalyticsAsync("Finance", username, fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving finance analytics: {ex.Message}");
            }
        }

        /// <summary>
        /// Get finance department dashboard
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult> GetFinanceDashboard()
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User not authenticated");

                var result = await _departmentReportService.GetDepartmentDashboardAsync("Finance", username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving finance dashboard: {ex.Message}");
            }
        }
    }
}
