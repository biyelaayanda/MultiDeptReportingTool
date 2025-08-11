using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.Models;
using MultiDeptReportingTool.Services;
using MultiDeptReportingTool.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MultiDeptReportingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseSeedController : ControllerBase
    {
        private readonly IPasswordService _passwordService;
        private readonly ComprehensiveDataSeedingService _seedingService;
        private readonly ILogger<DatabaseSeedController> _logger;
        private readonly ApplicationDbContext _context;

        public DatabaseSeedController(
            ComprehensiveDataSeedingService seedingService,
            ILogger<DatabaseSeedController> logger,
            ApplicationDbContext context,
            IPasswordService passwordService)
        {
            _seedingService = seedingService;
            _logger = logger;
            _context = context;
            _passwordService = passwordService;
        }

        [HttpPost("seed-comprehensive-data")]
        public async Task<IActionResult> SeedComprehensiveData()
        {
            try
            {
                _logger.LogInformation("Starting comprehensive data seeding via API...");
                await _seedingService.SeedComprehensiveDataAsync();
                
                return Ok(new { 
                    message = "Comprehensive data seeding completed successfully",
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during data seeding");
                return StatusCode(500, new { 
                    message = "Error occurred during data seeding", 
                    error = ex.Message 
                });
            }
        }

        [HttpPost("seed-ai-analytics-data")]
        public async Task<IActionResult> SeedAIAnalyticsData()
        {
            try
            {
                _logger.LogInformation("Starting AI analytics data seeding via API...");
                await SeedAIAnalyticsDataAsync();
                
                return Ok(new { 
                    message = "AI analytics data seeding completed successfully",
                    timestamp = DateTime.UtcNow,
                    details = "Generated historical data with anomalies, trends, and patterns for AI analysis"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during AI analytics data seeding");
                return StatusCode(500, new { 
                    message = "Error occurred during AI analytics data seeding", 
                    error = ex.Message 
                });
            }
        }

        [HttpPost("seed-initial-users")]
        public async Task<IActionResult> SeedInitialUsers()
        {
            try
            {
                _logger.LogInformation("Seeding initial users with Argon2id hashing...");

                // Remove foreign key constraints first
                var existingReports = await _context.Reports.ToListAsync();
                var existingAuditLogs = await _context.AuditLogs.ToListAsync();
                
                _context.Reports.RemoveRange(existingReports);
                _context.AuditLogs.RemoveRange(existingAuditLogs);
                await _context.SaveChangesAsync();

                // Now remove users
                var existingUsers = await _context.Users.ToListAsync();
                _context.Users.RemoveRange(existingUsers);
                await _context.SaveChangesAsync();

                // Create users with Argon2id hashed passwords
                var users = new List<Users>();
                
                // Executive Users
                var (ceoHash, ceoSalt) = await _passwordService.HashPasswordAsync("CEO123!");
                users.Add(new Users
                {
                    Username = "ceo",
                    Email = "ceo@example.com",
                    PasswordHash = ceoHash,
                    PasswordSalt = ceoSalt,
                    Role = "Executive",
                    FirstName = "John",
                    LastName = "Doe",
                    DepartmentId = 1, // Finance
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                
                var (execHash, execSalt) = await _passwordService.HashPasswordAsync("Executive123!");
                users.Add(new Users
                {
                    Username = "executive",
                    Email = "executive@example.com",
                    PasswordHash = execHash,
                    PasswordSalt = execSalt,
                    Role = "Executive",
                    FirstName = "Jane",
                    LastName = "Smith",
                    DepartmentId = 1, // Finance
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                
                // Department Leads
                var (financeLeadHash, financeLeadSalt) = await _passwordService.HashPasswordAsync("FinanceLead123!");
                users.Add(new Users
                {
                    Username = "finance-lead",
                    Email = "finance-lead@example.com",
                    PasswordHash = financeLeadHash,
                    PasswordSalt = financeLeadSalt,
                    Role = "DepartmentLead",
                    FirstName = "Michael",
                    LastName = "Johnson",
                    DepartmentId = 1, // Finance
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                
                var (hrLeadHash, hrLeadSalt) = await _passwordService.HashPasswordAsync("HRLead123!");
                users.Add(new Users
                {
                    Username = "hr-lead",
                    Email = "hr-lead@example.com",
                    PasswordHash = hrLeadHash,
                    PasswordSalt = hrLeadSalt,
                    Role = "DepartmentLead",
                    FirstName = "Sarah",
                    LastName = "Williams",
                    DepartmentId = 2, // HR
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                
                var (opsLeadHash, opsLeadSalt) = await _passwordService.HashPasswordAsync("OpsLead123!");
                users.Add(new Users
                {
                    Username = "operations-lead",
                    Email = "operations-lead@example.com",
                    PasswordHash = opsLeadHash,
                    PasswordSalt = opsLeadSalt,
                    Role = "DepartmentLead",
                    FirstName = "David",
                    LastName = "Brown",
                    DepartmentId = 3, // Operations
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                
                var (compLeadHash, compLeadSalt) = await _passwordService.HashPasswordAsync("CompLead123!");
                users.Add(new Users
                {
                    Username = "compliance-lead",
                    Email = "compliance-lead@example.com",
                    PasswordHash = compLeadHash,
                    PasswordSalt = compLeadSalt,
                    Role = "DepartmentLead",
                    FirstName = "Emma",
                    LastName = "Davis",
                    DepartmentId = 4, // Compliance
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                
                var (itLeadHash, itLeadSalt) = await _passwordService.HashPasswordAsync("ITLead123!");
                users.Add(new Users
                {
                    Username = "it-lead",
                    Email = "it-lead@example.com",
                    PasswordHash = itLeadHash,
                    PasswordSalt = itLeadSalt,
                    Role = "DepartmentLead",
                    FirstName = "Robert",
                    LastName = "Wilson",
                    DepartmentId = 5, // IT
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });

                // Staff Members
                var (financeStaffHash, financeStaffSalt) = await _passwordService.HashPasswordAsync("Finance123!");
                users.Add(new Users
                {
                    Username = "finance-staff",
                    Email = "finance-staff@example.com",
                    PasswordHash = financeStaffHash,
                    PasswordSalt = financeStaffSalt,
                    Role = "Staff",
                    FirstName = "Tom",
                    LastName = "Anderson",
                    DepartmentId = 1,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                
                var (hrStaffHash, hrStaffSalt) = await _passwordService.HashPasswordAsync("HR123!");
                users.Add(new Users
                {
                    Username = "hr-staff",
                    Email = "hr-staff@example.com",
                    PasswordHash = hrStaffHash,
                    PasswordSalt = hrStaffSalt,
                    Role = "Staff",
                    FirstName = "Lisa",
                    LastName = "Taylor",
                    DepartmentId = 2,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                
                var (opsStaffHash, opsStaffSalt) = await _passwordService.HashPasswordAsync("Ops123!");
                users.Add(new Users
                {
                    Username = "operations-staff",
                    Email = "operations-staff@example.com",
                    PasswordHash = opsStaffHash,
                    PasswordSalt = opsStaffSalt,
                    Role = "Staff",
                    FirstName = "Mark",
                    LastName = "Miller",
                    DepartmentId = 3,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                
                var (compStaffHash, compStaffSalt) = await _passwordService.HashPasswordAsync("Comp123!");
                users.Add(new Users
                {
                    Username = "compliance-staff",
                    Email = "compliance-staff@example.com",
                    PasswordHash = compStaffHash,
                    PasswordSalt = compStaffSalt,
                    Role = "Staff",
                    FirstName = "Anna",
                    LastName = "White",
                    DepartmentId = 4,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                
                var (itStaffHash, itStaffSalt) = await _passwordService.HashPasswordAsync("IT123!");
                users.Add(new Users
                {
                    Username = "it-staff",
                    Email = "it-staff@example.com",
                    PasswordHash = itStaffHash,
                    PasswordSalt = itStaffSalt,
                    Role = "Staff",
                    FirstName = "James",
                    LastName = "Lee",
                    DepartmentId = 5,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });

                await _context.Users.AddRangeAsync(users);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "All users seeded successfully with Argon2id hashing",
                    count = users.Count,
                    roles = users.Select(u => u.Role).Distinct().ToList(),
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding initial users");
                return StatusCode(500, new { 
                    message = "Error occurred while seeding initial users", 
                    error = ex.Message 
                });
            }
        }

        [HttpGet("verify-data")]
        public async Task<IActionResult> VerifySeededData()
        {
            try
            {
                var departmentCount = await _context.Departments.CountAsync();
                var userCount = await _context.Users.CountAsync();
                var reportCount = await _context.Reports.CountAsync();
                var reportDataCount = await _context.ReportData.CountAsync();
                var auditLogCount = await _context.AuditLogs.CountAsync();

                var departments = await _context.Departments.Select(d => d.Name).ToListAsync();
                var recentReports = await _context.Reports
                    .Include(r => r.Department)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .Select(r => new {
                        r.Title,
                        r.Status,
                        Department = r.Department.Name,
                        r.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    summary = new
                    {
                        departments = departmentCount,
                        users = userCount,
                        reports = reportCount,
                        reportData = reportDataCount,
                        auditLogs = auditLogCount
                    },
                    departmentNames = departments,
                    recentReports = recentReports,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying seeded data");
                return StatusCode(500, new { 
                    message = "Error verifying seeded data", 
                    error = ex.Message 
                });
            }
        }

        private async Task SeedAIAnalyticsDataAsync()
        {
            _logger.LogInformation("Starting AI analytics data seeding...");

            // Clear existing data for fresh start
            _context.AuditLogs.RemoveRange(_context.AuditLogs);
            _context.ReportData.RemoveRange(_context.ReportData);
            _context.Reports.RemoveRange(_context.Reports);
            await _context.SaveChangesAsync();

            var departments = await _context.Departments.ToListAsync();
            var users = await _context.Users.ToListAsync();

            if (!departments.Any() || !users.Any())
            {
                _logger.LogWarning("No departments or users found. Cannot seed AI analytics data.");
                return;
            }

            var random = new Random(123); // Fixed seed for consistent results
            var now = DateTime.UtcNow;
            var reports = new List<Report>();

            // Generate 18 months of historical data for better trends and anomalies
            for (int monthsBack = 18; monthsBack >= 0; monthsBack--)
            {
                var monthStart = now.AddMonths(-monthsBack).AddDays(-now.Day + 1);
                
                foreach (var department in departments)
                {
                    var departmentUsers = users.Where(u => u.DepartmentId == department.Id).ToList();
                    if (!departmentUsers.Any())
                        departmentUsers = users.Where(u => u.Role == "Executive").ToList();

                    // Generate 8-15 reports per month per department for good data volume
                    var reportsPerMonth = random.Next(8, 16);
                    
                    for (int reportIndex = 0; reportIndex < reportsPerMonth; reportIndex++)
                    {
                        var createdDate = monthStart.AddDays(random.Next(0, 28)).AddHours(random.Next(8, 18));
                        var reportTypeIndex = reportIndex % 4; // Cycle through different report types
                        
                        var report = new Report
                        {
                            Title = GetAIAnalyticsReportTitle(department.Name, reportTypeIndex, createdDate),
                            Status = GetReportStatus(monthsBack, random),
                            DepartmentId = department.Id,
                            CreatedByUserId = departmentUsers[random.Next(departmentUsers.Count)].Id,
                            CreatedAt = createdDate,
                            ReportType = GetReportType(reportTypeIndex),
                            Description = $"AI Analytics Report for {department.Name} - {createdDate:MMMM yyyy}",
                            ReportPeriodStart = createdDate.AddDays(-7),
                            ReportPeriodEnd = createdDate
                        };

                        reports.Add(report);
                    }
                }
            }

            await _context.Reports.AddRangeAsync(reports);
            await _context.SaveChangesAsync();

            // Generate rich report data with patterns and anomalies
            await SeedAIReportDataAsync(reports, random);
            
            // Generate audit logs for activity tracking
            await SeedAIAuditLogsAsync(reports, users, random);

            await _context.SaveChangesAsync();
            _logger.LogInformation($"AI analytics data seeding completed. Generated {reports.Count} reports with comprehensive data.");
        }

        private async Task SeedAIReportDataAsync(List<Report> reports, Random random)
        {
            var reportDataList = new List<ReportData>();
            var now = DateTime.UtcNow;

            foreach (var report in reports)
            {
                var monthsFromNow = (int)((now - report.CreatedAt).TotalDays / 30);
                var departmentName = _context.Departments.Find(report.DepartmentId)?.Name ?? "Unknown";

                // Generate data with realistic trends and occasional anomalies
                var baseEfficiency = GetBaseDepartmentEfficiency(departmentName);
                var baseBudget = GetBaseDepartmentBudget(departmentName);
                var baseWorkload = GetBaseDepartmentWorkload(departmentName);

                // Add seasonal variations and trends
                var seasonalMultiplier = GetSeasonalMultiplier(report.CreatedAt);
                var trendMultiplier = GetTrendMultiplier(monthsFromNow);
                var anomalyMultiplier = GetAnomalyMultiplier(random, monthsFromNow);

                // Create 15-25 data points per report for rich analysis
                var dataPointsCount = random.Next(15, 26);
                
                for (int i = 0; i < dataPointsCount; i++)
                {
                    var efficiency = Math.Round(baseEfficiency * seasonalMultiplier * trendMultiplier * anomalyMultiplier + random.Next(-5, 6), 2);
                    var budget = Math.Round(baseBudget * seasonalMultiplier * trendMultiplier * GetBudgetVariation(random), 2);
                    var workload = Math.Round(baseWorkload * seasonalMultiplier * trendMultiplier * GetWorkloadVariation(random), 2);
                    var completion = Math.Round(Math.Min(100, Math.Max(60, efficiency + random.Next(-10, 15))), 2);

                    // Ensure realistic ranges
                    efficiency = Math.Max(30, Math.Min(100, efficiency));
                    budget = Math.Max(5000, Math.Min(200000, budget));
                    workload = Math.Max(20, Math.Min(150, workload));

                    reportDataList.Add(new ReportData
                    {
                        ReportId = report.Id,
                        FieldName = "Efficiency",
                        FieldType = "Number",
                        NumericValue = (decimal)efficiency,
                        CreatedAt = report.CreatedAt.AddHours(i * 2)
                    });

                    reportDataList.Add(new ReportData
                    {
                        ReportId = report.Id,
                        FieldName = "Budget",
                        FieldType = "Currency",
                        NumericValue = (decimal)budget,
                        CreatedAt = report.CreatedAt.AddHours(i * 2)
                    });

                    reportDataList.Add(new ReportData
                    {
                        ReportId = report.Id,
                        FieldName = "Workload",
                        FieldType = "Number",
                        NumericValue = (decimal)workload,
                        CreatedAt = report.CreatedAt.AddHours(i * 2)
                    });

                    reportDataList.Add(new ReportData
                    {
                        ReportId = report.Id,
                        FieldName = "Completion",
                        FieldType = "Number",
                        NumericValue = (decimal)completion,
                        CreatedAt = report.CreatedAt.AddHours(i * 2)
                    });
                }
            }

            await _context.ReportData.AddRangeAsync(reportDataList);
        }

        private async Task SeedAIAuditLogsAsync(List<Report> reports, List<Users> users, Random random)
        {
            var auditLogs = new List<AuditLog>();

            foreach (var report in reports)
            {
                var actionsCount = random.Next(3, 8); // Multiple actions per report
                
                for (int i = 0; i < actionsCount; i++)
                {
                    var user = users[random.Next(users.Count)];
                    var actionTime = report.CreatedAt.AddHours(random.Next(1, 72));
                    var actions = new[] { "Created", "Updated", "Reviewed", "Approved", "Exported", "Shared", "Archived" };
                    
                    auditLogs.Add(new AuditLog
                    {
                        UserId = user.Id,
                        Action = actions[random.Next(actions.Length)],
                        EntityName = "Report",
                        EntityId = report.Id,
                        NewValues = $"User {user.Username} performed action on report {report.Title}",
                        Timestamp = actionTime,
                        IpAddress = $"192.168.1.{random.Next(1, 255)}",
                        UserAgent = "AI Analytics System"
                    });
                }
            }

            await _context.AuditLogs.AddRangeAsync(auditLogs);
        }

        // Helper methods for AI analytics data generation
        private string GetAIAnalyticsReportTitle(string departmentName, int typeIndex, DateTime date)
        {
            var reportTypes = new[]
            {
                "Performance Analytics Report",
                "Efficiency Optimization Report", 
                "Budget Analysis Report",
                "Workload Distribution Report"
            };
            return $"{departmentName} {reportTypes[typeIndex]} - {date:MMM yyyy}";
        }

        private string GetReportStatus(int monthsBack, Random random)
        {
            // More recent reports more likely to be pending
            if (monthsBack <= 1) return random.Next(100) < 40 ? "Pending" : "Approved";
            if (monthsBack <= 3) return random.Next(100) < 20 ? "Pending" : "Approved";
            return "Approved"; // Older reports are mostly approved
        }

        private string GetReportType(int typeIndex)
        {
            var types = new[] { "Analytics", "Performance", "Financial", "Operational" };
            return types[typeIndex];
        }

        private double GetBaseDepartmentEfficiency(string departmentName)
        {
            return departmentName switch
            {
                "Finance" => 85.0,
                "Human Resources" => 78.0,
                "Operations" => 82.0,
                "Compliance" => 88.0,
                "Information Technology" => 90.0,
                _ => 80.0
            };
        }

        private double GetBaseDepartmentBudget(string departmentName)
        {
            return departmentName switch
            {
                "Finance" => 45000.0,
                "Human Resources" => 32000.0,
                "Operations" => 55000.0,
                "Compliance" => 28000.0,
                "Information Technology" => 65000.0,
                _ => 40000.0
            };
        }

        private double GetBaseDepartmentWorkload(string departmentName)
        {
            return departmentName switch
            {
                "Finance" => 85.0,
                "Human Resources" => 75.0,
                "Operations" => 95.0,
                "Compliance" => 70.0,
                "Information Technology" => 100.0,
                _ => 80.0
            };
        }

        private double GetSeasonalMultiplier(DateTime date)
        {
            var month = date.Month;
            // Simulate seasonal business patterns
            return month switch
            {
                12 or 1 => 0.85, // Holiday slowdown
                2 or 3 => 1.1,   // Post-holiday surge
                6 or 7 => 0.95,  // Summer vacation impact
                9 or 10 => 1.15, // Back-to-school/business surge
                _ => 1.0
            };
        }

        private double GetTrendMultiplier(int monthsFromNow)
        {
            // Simulate gradual improvement over time with occasional setbacks
            var baseTrend = 1.0 + (monthsFromNow * 0.01); // 1% improvement per month
            
            // Add some volatility
            if (monthsFromNow == 6 || monthsFromNow == 12) return baseTrend * 0.85; // Setbacks
            if (monthsFromNow == 3 || monthsFromNow == 15) return baseTrend * 1.2;  // Breakthroughs
            
            return baseTrend;
        }

        private double GetAnomalyMultiplier(Random random, int monthsFromNow)
        {
            // 5% chance of significant anomaly
            if (random.Next(100) < 5)
            {
                return random.NextDouble() * 0.4 + 0.6; // 60%-100% of normal (significant drop)
            }
            
            // 10% chance of minor anomaly  
            if (random.Next(100) < 10)
            {
                return random.NextDouble() * 0.3 + 0.85; // 85%-115% of normal
            }
            
            return 1.0; // Normal operation
        }

        private double GetBudgetVariation(Random random)
        {
            return 0.8 + (random.NextDouble() * 0.4); // 80%-120% variation
        }

        private double GetWorkloadVariation(Random random)
        {
            return 0.7 + (random.NextDouble() * 0.6); // 70%-130% variation
        }
    }
}
