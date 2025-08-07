using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Services.Export;
using MultiDeptReportingTool.DTOs.Export;
using MultiDeptReportingTool.DTOs.Analytics;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.Models;

namespace MultiDeptReportingTool.Controllers
{
    /// <summary>
    /// Export controller for generating and managing report exports
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExportController : ControllerBase
    {
        private readonly IExportService _exportService;
        private readonly ILogger<ExportController> _logger;
        private readonly ApplicationDbContext _context;

        public ExportController(IExportService exportService, ILogger<ExportController> logger, ApplicationDbContext context)
        {
            _exportService = exportService;
            _logger = logger;
            _context = context;
        }

        private async Task<Users?> GetCurrentUserAsync()
        {
            var userId = User.FindFirst("sub")?.Value ?? 
                        User.FindFirst("userId")?.Value ?? 
                        User.FindFirst("id")?.Value ??
                        User.Identity?.Name;
            
            if (string.IsNullOrEmpty(userId))
                return null;

            if (int.TryParse(userId, out int userIdInt))
            {
                return await _context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == userIdInt);
            }
            
            return await _context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Username == userId || u.Email == userId);
        }

        private async Task<bool> CanUserAccessDepartment(Users user, int departmentId)
        {
            // Admins and Executives can access all departments
            if (user.Role == "Admin" || user.Role == "Executive")
                return true;
            
            // Department leads and staff can only access their own department
            return user.DepartmentId == departmentId;
        }

        private async Task<bool> CanUserAccessReport(Users user, int reportId)
        {
            var report = await _context.Reports.Include(r => r.Department).FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null) return false;

            // Admins and Executives can access all reports
            if (user.Role == "Admin" || user.Role == "Executive")
                return true;
            
            // Users can only access reports from their department
            return user.DepartmentId == report.DepartmentId;
        }

        #region Export Endpoints

        /// <summary>
        /// Export report to PDF
        /// </summary>
        [HttpPost("pdf")]
        public async Task<ActionResult> ExportToPdf([FromBody] ExportRequestDto request)
        {
            try
            {
                var result = await _exportService.ExportToPdfAsync(request);
                
                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message });
                }

                return File(result.FileData, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to PDF");
                return StatusCode(500, new { message = "Error exporting to PDF", error = ex.Message });
            }
        }

        /// <summary>
        /// Export report to Excel
        /// </summary>
        [HttpPost("excel")]
        public async Task<ActionResult> ExportToExcel([FromBody] ExportRequestDto request)
        {
            try
            {
                var result = await _exportService.ExportToExcelAsync(request);
                
                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message });
                }

                return File(result.FileData, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to Excel");
                return StatusCode(500, new { message = "Error exporting to Excel", error = ex.Message });
            }
        }

        /// <summary>
        /// Export report to CSV
        /// </summary>
        [HttpPost("csv")]
        public async Task<ActionResult> ExportToCsv([FromBody] ExportRequestDto request)
        {
            try
            {
                var result = await _exportService.ExportToCsvAsync(request);
                
                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message });
                }

                return File(result.FileData, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to CSV");
                return StatusCode(500, new { message = "Error exporting to CSV", error = ex.Message });
            }
        }

        /// <summary>
        /// Export report to JSON
        /// </summary>
        [HttpPost("json")]
        public async Task<ActionResult> ExportToJson([FromBody] ExportRequestDto request)
        {
            try
            {
                var result = await _exportService.ExportToJsonAsync(request);
                
                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message });
                }

                return File(result.FileData, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to JSON");
                return StatusCode(500, new { message = "Error exporting to JSON", error = ex.Message });
            }
        }

        /// <summary>
        /// Export report to PowerPoint
        /// </summary>
        [HttpPost("powerpoint")]
        public async Task<ActionResult> ExportToPowerPoint([FromBody] ExportRequestDto request)
        {
            try
            {
                var result = await _exportService.ExportToPowerPointAsync(request);
                
                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message });
                }

                return File(result.FileData, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to PowerPoint");
                return StatusCode(500, new { message = "Error exporting to PowerPoint", error = ex.Message });
            }
        }

        /// <summary>
        /// Generic export endpoint that handles multiple formats
        /// </summary>
        [HttpPost("generate")]
        public async Task<ActionResult> GenerateExport([FromBody] ExportRequestDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Format))
                {
                    return BadRequest(new { message = "Export request and format are required" });
                }

                // Get current user context
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                // Validate user access to requested data
                if (request.ReportIds?.Any() == true)
                {
                    foreach (var reportId in request.ReportIds)
                    {
                        if (!await CanUserAccessReport(currentUser, reportId))
                        {
                            return Forbid($"You don't have access to report {reportId}");
                        }
                    }
                }

                // Filter departments based on user access
                if (request.Departments?.Any() == true)
                {
                    var accessibleDepartments = new List<string>();
                    
                    foreach (var deptName in request.Departments)
                    {
                        var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Name == deptName);
                        if (dept != null && await CanUserAccessDepartment(currentUser, dept.Id))
                        {
                            accessibleDepartments.Add(deptName);
                        }
                    }
                    
                    request.Departments = accessibleDepartments;
                    
                    if (!accessibleDepartments.Any())
                    {
                        return Forbid("You don't have access to any of the requested departments");
                    }
                }
                else if (currentUser.Role == "DepartmentLead" || currentUser.Role == "Staff")
                {
                    // For department leads and staff, limit to their department only
                    if (currentUser.Department != null)
                    {
                        request.Departments = new List<string> { currentUser.Department.Name };
                    }
                }

                // Add user context to the request
                request.UserId = currentUser.Id;
                request.UserRole = currentUser.Role;
                request.UserDepartment = currentUser.Department?.Name;

                ExportResponseDto result = request.Format.ToLower() switch
                {
                    "pdf" => await _exportService.ExportToPdfAsync(request),
                    "csv" => await _exportService.ExportToCsvAsync(request),
                    "json" => await _exportService.ExportToJsonAsync(request),
                    "powerpoint" or "ppt" or "pptx" => await _exportService.ExportToPowerPointAsync(request),
                    _ => new ExportResponseDto 
                    { 
                        Success = false, 
                        Message = $"Unsupported format: {request.Format}" 
                    }
                };

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message });
                }

                return File(result.FileData, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating export");
                return StatusCode(500, new { message = "Error generating export", error = ex.Message });
            }
        }

        /// <summary>
        /// Export executive dashboard to PDF
        /// </summary>
        [HttpPost("dashboard/pdf")]
        public async Task<ActionResult> ExportDashboardToPdf([FromBody] ExecutiveDashboardDto dashboard)
        {
            try
            {
                if (dashboard == null)
                {
                    return BadRequest(new { message = "Dashboard data is required" });
                }

                var pdfBytes = await _exportService.GenerateDashboardPdfAsync(dashboard);
                var fileName = $"Executive_Dashboard_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting dashboard to PDF");
                return StatusCode(500, new { message = "Error exporting dashboard to PDF", error = ex.Message });
            }
        }

        #endregion

        #region Chart Generation Endpoints

        /// <summary>
        /// Generate chart image
        /// </summary>
        [HttpPost("charts/generate")]
        public async Task<ActionResult> GenerateChart([FromBody] ChartConfigDto chartConfig)
        {
            try
            {
                var errors = new List<string>();
                if (!_exportService.ValidateChartConfig(chartConfig, out errors))
                {
                    return BadRequest(new { message = "Invalid chart configuration", errors });
                }

                var chartBytes = await _exportService.GenerateChartImageAsync(chartConfig);
                var fileName = $"Chart_{chartConfig.Title?.Replace(" ", "_") ?? "Untitled"}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";

                return File(chartBytes, "image/png", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating chart");
                return StatusCode(500, new { message = "Error generating chart", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate multiple charts
        /// </summary>
        [HttpPost("charts/multiple")]
        public async Task<ActionResult> GenerateMultipleCharts([FromBody] List<ChartConfigDto> chartConfigs)
        {
            try
            {
                if (chartConfigs == null || !chartConfigs.Any())
                {
                    return BadRequest(new { message = "Chart configurations are required" });
                }

                var chartImages = await _exportService.GenerateMultipleChartsAsync(chartConfigs);
                
                // For multiple charts, we'll return a ZIP file or combined document
                // For now, return the first chart as an example
                if (chartImages.Any())
                {
                    var fileName = $"Charts_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
                    return File(chartImages.First(), "image/png", fileName);
                }

                return NotFound(new { message = "No charts generated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating multiple charts");
                return StatusCode(500, new { message = "Error generating multiple charts", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate dashboard charts
        /// </summary>
        [HttpPost("charts/dashboard")]
        public async Task<ActionResult> GenerateDashboardCharts([FromBody] ExecutiveDashboardDto dashboard)
        {
            try
            {
                if (dashboard == null)
                {
                    return BadRequest(new { message = "Dashboard data is required" });
                }

                var chartsBytes = await _exportService.GenerateDashboardChartsAsync(dashboard);
                var fileName = $"Dashboard_Charts_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";

                return File(chartsBytes, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dashboard charts");
                return StatusCode(500, new { message = "Error generating dashboard charts", error = ex.Message });
            }
        }

        #endregion

        #region Email Endpoints

        /// <summary>
        /// Send email notification
        /// </summary>
        [HttpPost("email/send")]
        public async Task<ActionResult> SendEmail([FromBody] EmailNotificationDto emailDto)
        {
            try
            {
                var errors = new List<string>();
                if (!_exportService.ValidateEmailRequest(emailDto, out errors))
                {
                    return BadRequest(new { message = "Invalid email request", errors });
                }

                var success = await _exportService.SendEmailAsync(emailDto);
                
                if (success)
                {
                    return Ok(new { message = "Email sent successfully" });
                }
                
                return StatusCode(500, new { message = "Failed to send email" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                return StatusCode(500, new { message = "Error sending email", error = ex.Message });
            }
        }

        /// <summary>
        /// Send email with export attachment
        /// </summary>
        [HttpPost("email/send-with-export")]
        public async Task<ActionResult> SendEmailWithExport([FromBody] EmailWithExportRequestDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ToEmail) || request.ExportRequest == null)
                {
                    return BadRequest(new { message = "Email recipient and export request are required" });
                }

                // First generate the export
                var exportResult = await _exportService.ExportToPdfAsync(request.ExportRequest);
                
                if (!exportResult.Success)
                {
                    return BadRequest(new { message = $"Failed to generate export: {exportResult.Message}" });
                }

                // Then send email with attachment
                var success = await _exportService.SendEmailWithAttachmentAsync(
                    request.ToEmail,
                    request.Subject ?? "Report Export",
                    request.Body ?? "Please find the attached report.",
                    exportResult);

                if (success)
                {
                    return Ok(new { message = "Email with export sent successfully" });
                }
                
                return StatusCode(500, new { message = "Failed to send email with export" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email with export");
                return StatusCode(500, new { message = "Error sending email with export", error = ex.Message });
            }
        }

        /// <summary>
        /// Send bulk emails
        /// </summary>
        [HttpPost("email/bulk")]
        public async Task<ActionResult> SendBulkEmail([FromBody] List<EmailNotificationDto> emails)
        {
            try
            {
                if (emails == null || !emails.Any())
                {
                    return BadRequest(new { message = "Email list is required" });
                }

                var success = await _exportService.SendBulkEmailAsync(emails);
                
                if (success)
                {
                    return Ok(new { message = $"Bulk email sent successfully to {emails.Count} recipients" });
                }
                
                return StatusCode(500, new { message = "Some emails failed to send" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk email");
                return StatusCode(500, new { message = "Error sending bulk email", error = ex.Message });
            }
        }

        #endregion

        #region Scheduled Reports Endpoints

        /// <summary>
        /// Get all scheduled reports
        /// </summary>
        [HttpGet("scheduled")]
        public async Task<ActionResult<List<ScheduledReportDto>>> GetScheduledReports()
        {
            try
            {
                var userId = User.Identity?.Name;
                var reports = await _exportService.GetScheduledReportsAsync(userId);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scheduled reports");
                return StatusCode(500, new { message = "Error retrieving scheduled reports", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new scheduled report
        /// </summary>
        [HttpPost("scheduled")]
        public async Task<ActionResult<ScheduledReportDto>> CreateScheduledReport([FromBody] ScheduledReportDto scheduledReport)
        {
            try
            {
                if (scheduledReport == null || string.IsNullOrWhiteSpace(scheduledReport.Name))
                {
                    return BadRequest(new { message = "Scheduled report name is required" });
                }

                scheduledReport.CreatedBy = User.Identity?.Name ?? "biyelaayanda3@gmail.com";
                var createdReport = await _exportService.CreateScheduledReportAsync(scheduledReport);
                
                return CreatedAtAction(nameof(GetScheduledReports), new { id = createdReport.Id }, createdReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating scheduled report");
                return StatusCode(500, new { message = "Error creating scheduled report", error = ex.Message });
            }
        }

        /// <summary>
        /// Update a scheduled report
        /// </summary>
        [HttpPut("scheduled/{id}")]
        public async Task<ActionResult> UpdateScheduledReport(int id, [FromBody] ScheduledReportDto scheduledReport)
        {
            try
            {
                if (scheduledReport == null)
                {
                    return BadRequest(new { message = "Scheduled report data is required" });
                }

                var success = await _exportService.UpdateScheduledReportAsync(id, scheduledReport);
                
                if (success)
                {
                    return Ok(new { message = "Scheduled report updated successfully" });
                }
                
                return NotFound(new { message = "Scheduled report not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating scheduled report");
                return StatusCode(500, new { message = "Error updating scheduled report", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a scheduled report
        /// </summary>
        [HttpDelete("scheduled/{id}")]
        public async Task<ActionResult> DeleteScheduledReport(int id)
        {
            try
            {
                var success = await _exportService.DeleteScheduledReportAsync(id);
                
                if (success)
                {
                    return Ok(new { message = "Scheduled report deleted successfully" });
                }
                
                return NotFound(new { message = "Scheduled report not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting scheduled report");
                return StatusCode(500, new { message = "Error deleting scheduled report", error = ex.Message });
            }
        }

        /// <summary>
        /// Run a scheduled report immediately
        /// </summary>
        [HttpPost("scheduled/{id}/run")]
        public async Task<ActionResult> RunScheduledReport(int id)
        {
            try
            {
                var success = await _exportService.RunScheduledReportAsync(id);
                
                if (success)
                {
                    return Ok(new { message = "Scheduled report executed successfully" });
                }
                
                return NotFound(new { message = "Scheduled report not found or failed to execute" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running scheduled report");
                return StatusCode(500, new { message = "Error running scheduled report", error = ex.Message });
            }
        }

        /// <summary>
        /// Get due scheduled reports
        /// </summary>
        [HttpGet("scheduled/due")]
        public async Task<ActionResult<List<ScheduledReportDto>>> GetDueScheduledReports()
        {
            try
            {
                var reports = await _exportService.GetDueScheduledReportsAsync();
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting due scheduled reports");
                return StatusCode(500, new { message = "Error retrieving due scheduled reports", error = ex.Message });
            }
        }

        #endregion

        #region Template Management Endpoints

        /// <summary>
        /// Get email template
        /// </summary>
        [HttpGet("templates/email/{templateName}")]
        public async Task<ActionResult<string>> GetEmailTemplate(
            string templateName,
            [FromQuery] Dictionary<string, string> parameters = null)
        {
            try
            {
                var paramDict = parameters?.ToDictionary(p => p.Key, p => (object)p.Value) ?? new Dictionary<string, object>();
                var template = await _exportService.GetEmailTemplateAsync(templateName, paramDict);
                
                return Ok(new { template });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email template");
                return StatusCode(500, new { message = "Error retrieving email template", error = ex.Message });
            }
        }

        /// <summary>
        /// Get report template
        /// </summary>
        [HttpGet("templates/report/{templateName}")]
        public async Task<ActionResult<string>> GetReportTemplate(
            string templateName,
            [FromQuery] Dictionary<string, string> parameters = null)
        {
            try
            {
                var paramDict = parameters?.ToDictionary(p => p.Key, p => (object)p.Value) ?? new Dictionary<string, object>();
                var template = await _exportService.GetReportTemplateAsync(templateName, paramDict);
                
                return Ok(new { template });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report template");
                return StatusCode(500, new { message = "Error retrieving report template", error = ex.Message });
            }
        }

        /// <summary>
        /// Save custom template
        /// </summary>
        [HttpPost("templates/custom")]
        public async Task<ActionResult> SaveCustomTemplate([FromBody] CustomTemplateRequestDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.TemplateName) || string.IsNullOrWhiteSpace(request.TemplateContent))
                {
                    return BadRequest(new { message = "Template name and content are required" });
                }

                var success = await _exportService.SaveCustomTemplateAsync(request.TemplateName, request.TemplateContent);
                
                if (success)
                {
                    return Ok(new { message = "Custom template saved successfully" });
                }
                
                return StatusCode(500, new { message = "Failed to save custom template" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving custom template");
                return StatusCode(500, new { message = "Error saving custom template", error = ex.Message });
            }
        }

        #endregion

        #region File Management Endpoints

        /// <summary>
        /// Download export file
        /// </summary>
        [HttpGet("download/{fileName}")]
        public async Task<ActionResult> DownloadFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "exports", fileName);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = "File not found" });
                }

                var fileBytes = await _exportService.GetExportFileAsync(filePath);
                var contentType = GetContentType(fileName);
                
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file");
                return StatusCode(500, new { message = "Error downloading file", error = ex.Message });
            }
        }

        /// <summary>
        /// Get export history
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<List<string>>> GetExportHistory([FromQuery] int limit = 50)
        {
            try
            {
                var userId = User.Identity?.Name ?? "current_user";
                var history = await _exportService.GetExportHistoryAsync(userId, limit);
                
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting export history");
                return StatusCode(500, new { message = "Error retrieving export history", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete export file
        /// </summary>
        [HttpDelete("files/{fileName}")]
        public async Task<ActionResult> DeleteExportFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "exports", fileName);
                var success = await _exportService.DeleteExportFileAsync(filePath);
                
                if (success)
                {
                    return Ok(new { message = "File deleted successfully" });
                }
                
                return NotFound(new { message = "File not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting export file");
                return StatusCode(500, new { message = "Error deleting export file", error = ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".csv" => "text/csv",
                ".json" => "application/json",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        #endregion
    }

    #region Request DTOs

    public class EmailWithExportRequestDto
    {
        public string ToEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public ExportRequestDto ExportRequest { get; set; } = new ExportRequestDto();
    }

    public class CustomTemplateRequestDto
    {
        public string TemplateName { get; set; } = string.Empty;
        public string TemplateContent { get; set; } = string.Empty;
    }

    #endregion
}
