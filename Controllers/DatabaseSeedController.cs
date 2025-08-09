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
    }
}
