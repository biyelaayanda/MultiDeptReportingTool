using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Services;
using System.Security.Claims;

namespace MultiDeptReportingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetReports([FromQuery] ReportFilterDto filter)
        {
            try
            {
                var (reports, totalCount) = await _reportService.GetReportsAsync(filter);

                return Ok(new
                {
                    success = true,
                    data = reports,
                    totalCount,
                    page = filter.Page,
                    pageSize = filter.PageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("types")]
        public IActionResult GetReportTypes()
        {
            try
            {
                var reportTypes = new[]
                {
                    "Monthly Report",
                    "Quarterly Report", 
                    "Annual Report",
                    "Financial Report",
                    "Performance Report",
                    "Sales Report",
                    "Marketing Report",
                    "Operations Report",
                    "HR Report",
                    "Custom Report"
                };

                return Ok(reportTypes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReport(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();
                var userDepartmentId = GetCurrentUserDepartmentId();

                // Check access permissions
                if (!await _reportService.CanUserAccessReportAsync(id, userId, userRole, userDepartmentId))
                {
                    return Forbid();
                }

                var report = await _reportService.GetReportByIdAsync(id);
                if (report == null)
                {
                    return NotFound(new { success = false, message = "Report not found" });
                }

                return Ok(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto createReportDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                var report = await _reportService.CreateReportAsync(createReportDto, userId);

                if (report == null)
                {
                    return BadRequest(new { success = false, message = "Failed to create report" });
                }

                return CreatedAtAction(nameof(GetReport), new { id = report.Id }, 
                    new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReport(int id, [FromBody] UpdateReportDto updateReportDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                var report = await _reportService.UpdateReportAsync(id, updateReportDto, userId);

                if (report == null)
                {
                    return NotFound(new { success = false, message = "Report not found or cannot be updated" });
                }

                return Ok(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _reportService.DeleteReportAsync(id, userId);

                if (!result)
                {
                    return NotFound(new { success = false, message = "Report not found or cannot be deleted" });
                }

                return Ok(new { success = true, message = "Report deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("{id}/submit")]
        public async Task<IActionResult> SubmitReport(int id, [FromBody] ReportSubmissionDto submissionDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var report = await _reportService.SubmitReportAsync(id, submissionDto, userId);

                if (report == null)
                {
                    return NotFound(new { success = false, message = "Report not found or cannot be submitted" });
                }

                return Ok(new { success = true, data = report, message = "Report submitted for approval" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin,Executive,DepartmentLead")]
        public async Task<IActionResult> ApproveReport(int id, [FromBody] ReportApprovalDto approvalDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var report = await _reportService.ApproveReportAsync(id, approvalDto, userId);

                if (report == null)
                {
                    return NotFound(new { success = false, message = "Report not found or cannot be approved" });
                }

                return Ok(new { success = true, data = report, message = "Report approved successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin,Executive,DepartmentLead")]
        public async Task<IActionResult> RejectReport(int id, [FromBody] ReportApprovalDto rejectionDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var report = await _reportService.RejectReportAsync(id, rejectionDto, userId);

                if (report == null)
                {
                    return NotFound(new { success = false, message = "Report not found or cannot be rejected" });
                }

                return Ok(new { success = true, data = report, message = "Report rejected and sent back for revision" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("{id}/data")]
        public async Task<IActionResult> GetReportData(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();
                var userDepartmentId = GetCurrentUserDepartmentId();

                // Check access permissions
                if (!await _reportService.CanUserAccessReportAsync(id, userId, userRole, userDepartmentId))
                {
                    return Forbid();
                }

                var reportData = await _reportService.GetReportDataAsync(id);
                return Ok(new { success = true, data = reportData });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPut("{id}/data")]
        public async Task<IActionResult> UpdateReportData(int id, [FromBody] List<UpdateReportDataDto> reportData)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _reportService.UpdateReportDataAsync(id, reportData, userId);

                if (!result)
                {
                    return NotFound(new { success = false, message = "Report not found or cannot be updated" });
                }

                var updatedData = await _reportService.GetReportDataAsync(id);
                return Ok(new { success = true, data = updatedData, message = "Report data updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        // Helper methods to get current user information
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        }

        private int? GetCurrentUserDepartmentId()
        {
            var deptClaim = User.FindFirst("DepartmentId");
            return deptClaim != null && int.TryParse(deptClaim.Value, out int deptId) ? deptId : null;
        }
    }
}
