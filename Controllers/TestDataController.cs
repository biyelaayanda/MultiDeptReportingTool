using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.Models;
using MultiDeptReportingTool.Services;

namespace MultiDeptReportingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestDataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TestDataController> _logger;

        public TestDataController(ApplicationDbContext context, ILogger<TestDataController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("seed-analytics-data")]
        public async Task<IActionResult> SeedAnalyticsData()
        {
            try
            {
                _logger.LogInformation("Starting analytics data seeding via API...");
                var seedingLogger = new LoggerFactory().CreateLogger<DatabaseSeedingService>();
                var seedingService = new DatabaseSeedingService(_context, seedingLogger);
                await seedingService.SeedAnalyticsDataAsync();
                
                // Get summary of seeded data
                var summary = new
                {
                    message = "Analytics data seeded successfully",
                    data = new
                    {
                        totalReports = await _context.Reports.CountAsync(),
                        totalReportData = await _context.ReportData.CountAsync(),
                        totalAuditLogs = await _context.AuditLogs.CountAsync(),
                        departments = await _context.Departments.CountAsync(),
                        users = await _context.Users.CountAsync()
                    },
                    timestamp = DateTime.UtcNow
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed analytics data");
                return StatusCode(500, new { 
                    message = "Failed to seed analytics data", 
                    error = ex.Message 
                });
            }
        }

        [HttpPost("seed")]
        public async Task<IActionResult> SeedTestData()
        {
            try
            {
                // Only seed if no reports exist
                if (await _context.Reports.AnyAsync())
                {
                    return Ok(new { message = "Test data already exists" });
                }

                // Create some test reports
                var testReports = new List<Report>
                {
                    new Report
                    {
                        Title = "Q4 Financial Report",
                        Description = "Fourth quarter financial summary",
                        ReportType = "Financial",
                        Status = "Completed",
                        DepartmentId = 1, // Finance
                        CreatedByUserId = 8, // Executive user
                        CreatedAt = DateTime.UtcNow.AddDays(-30),
                        ReportPeriodStart = DateTime.UtcNow.AddDays(-90),
                        ReportPeriodEnd = DateTime.UtcNow.AddDays(-60),
                        SubmittedAt = DateTime.UtcNow.AddDays(-25)
                    },
                    new Report
                    {
                        Title = "HR Performance Review",
                        Description = "Annual performance review summary",
                        ReportType = "Performance",
                        Status = "Completed",
                        DepartmentId = 2, // HR
                        CreatedByUserId = 8,
                        CreatedAt = DateTime.UtcNow.AddDays(-20),
                        ReportPeriodStart = DateTime.UtcNow.AddDays(-60),
                        ReportPeriodEnd = DateTime.UtcNow.AddDays(-30),
                        SubmittedAt = DateTime.UtcNow.AddDays(-15)
                    },
                    new Report
                    {
                        Title = "IT Infrastructure Report",
                        Description = "Technology infrastructure assessment",
                        ReportType = "Technical",
                        Status = "Pending",
                        DepartmentId = 5, // IT
                        CreatedByUserId = 8,
                        CreatedAt = DateTime.UtcNow.AddDays(-10),
                        ReportPeriodStart = DateTime.UtcNow.AddDays(-30),
                        ReportPeriodEnd = DateTime.UtcNow
                    },
                    new Report
                    {
                        Title = "Operations Efficiency Report",
                        Description = "Monthly operations efficiency analysis",
                        ReportType = "Operations",
                        Status = "Completed",
                        DepartmentId = 3, // Operations
                        CreatedByUserId = 8,
                        CreatedAt = DateTime.UtcNow.AddDays(-15),
                        ReportPeriodStart = DateTime.UtcNow.AddDays(-45),
                        ReportPeriodEnd = DateTime.UtcNow.AddDays(-15),
                        SubmittedAt = DateTime.UtcNow.AddDays(-10)
                    },
                    new Report
                    {
                        Title = "Compliance Audit Report",
                        Description = "Quarterly compliance audit findings",
                        ReportType = "Compliance",
                        Status = "Overdue",
                        DepartmentId = 4, // Compliance
                        CreatedByUserId = 8,
                        CreatedAt = DateTime.UtcNow.AddDays(-45),
                        ReportPeriodStart = DateTime.UtcNow.AddDays(-90),
                        ReportPeriodEnd = DateTime.UtcNow.AddDays(-60)
                    }
                };

                await _context.Reports.AddRangeAsync(testReports);
                await _context.SaveChangesAsync();

                // Create some test report data
                var report1 = testReports[0];
                var testReportData = new List<ReportData>
                {
                    new ReportData
                    {
                        ReportId = report1.Id,
                        FieldName = "Total Revenue",
                        FieldType = "Currency",
                        NumericValue = 1250000,
                        FieldValue = "$1,250,000"
                    },
                    new ReportData
                    {
                        ReportId = report1.Id,
                        FieldName = "Total Expenses",
                        FieldType = "Currency",
                        NumericValue = 980000,
                        FieldValue = "$980,000"
                    },
                    new ReportData
                    {
                        ReportId = report1.Id,
                        FieldName = "Net Profit",
                        FieldType = "Currency",
                        NumericValue = 270000,
                        FieldValue = "$270,000"
                    }
                };

                await _context.ReportData.AddRangeAsync(testReportData);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Test data created successfully",
                    reportsCreated = testReports.Count,
                    dataPointsCreated = testReportData.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetDataStatus()
        {
            var reportCount = await _context.Reports.CountAsync();
            var departmentCount = await _context.Departments.CountAsync();
            var userCount = await _context.Users.CountAsync();
            var dataPointCount = await _context.ReportData.CountAsync();

            return Ok(new {
                reports = reportCount,
                departments = departmentCount,
                users = userCount,
                dataPoints = dataPointCount
            });
        }

        [HttpGet("debug")]
        public async Task<IActionResult> DebugAnalytics()
        {
            try
            {
                var reports = await _context.Reports
                    .Include(r => r.Department)
                    .Include(r => r.CreatedByUser)
                    .ToListAsync();

                var completedReports = reports.Count(r => r.Status == "Completed");
                var pendingReports = reports.Count(r => r.Status == "Pending");
                var overdueReports = reports.Count(r => r.Status == "Overdue");

                return Ok(new {
                    totalReports = reports.Count,
                    completedReports,
                    pendingReports,
                    overdueReports,
                    reports = reports.Select(r => new {
                        id = r.Id,
                        title = r.Title,
                        status = r.Status,
                        department = r.Department?.Name ?? "Unknown",
                        createdBy = r.CreatedByUser?.Username ?? "Unknown"
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpDelete("clear-analytics-data")]
        public async Task<IActionResult> ClearAnalyticsData()
        {
            try
            {
                _logger.LogInformation("Clearing analytics data...");
                
                // Remove in order to respect foreign key constraints
                _context.AuditLogs.RemoveRange(_context.AuditLogs);
                _context.ReportData.RemoveRange(_context.ReportData);
                _context.Reports.RemoveRange(_context.Reports);
                
                await _context.SaveChangesAsync();
                
                return Ok(new { 
                    message = "Analytics data cleared successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear analytics data");
                return StatusCode(500, new { 
                    message = "Failed to clear analytics data", 
                    error = ex.Message 
                });
            }
        }
    }
}
