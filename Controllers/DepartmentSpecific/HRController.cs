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

        [HttpGet("debug/auth")]
        [AllowAnonymous]
        public ActionResult GetAuthenticationDebug()
        {
            return Ok(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                AuthenticationType = User.Identity?.AuthenticationType,
                Name = User.Identity?.Name,
                Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                Headers = Request.Headers.Select(h => new { h.Key, Value = h.Value.ToString() }).ToList()
            });
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
                {
                    return BadRequest(new { 
                        error = "User not authenticated", 
                        details = "Username not found in claims",
                        claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
                    });
                }

                var result = await _departmentReportService.GetHRReportsAsync(username, status, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    error = "Error retrieving HR reports", 
                    message = ex.Message,
                    stackTrace = ex.StackTrace 
                });
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
                {
                    return BadRequest(new { 
                        error = "User not authenticated", 
                        details = "Username not found in claims",
                        claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
                    });
                }

                var result = await _departmentReportService.GetDepartmentAnalyticsAsync("Human Resources", username, fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    error = "Error retrieving HR analytics", 
                    message = ex.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult> GetHRDashboard()
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(username))
                {
                    return BadRequest(new { 
                        error = "User not authenticated", 
                        details = "Username not found in claims",
                        claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
                    });
                }

                var result = await _departmentReportService.GetDepartmentDashboardAsync("Human Resources", username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    error = "Error retrieving HR dashboard", 
                    message = ex.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }
    }
}
