using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.Models;
using MultiDeptReportingTool.Services;
using System.Security.Cryptography;
using System.Text;

namespace MultiDeptReportingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseSeedController : ControllerBase
    {
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
        private readonly ComprehensiveDataSeedingService _seedingService;
        private readonly ILogger<DatabaseSeedController> _logger;
        private readonly ApplicationDbContext _context;

        public DatabaseSeedController(
            ComprehensiveDataSeedingService seedingService,
            ILogger<DatabaseSeedController> logger,
            ApplicationDbContext context)
        {
            _seedingService = seedingService;
            _logger = logger;
            _context = context;
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
                _logger.LogInformation("Seeding initial users...");

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

                var users = new List<Users>
                {
                    // Executive Users
                    new Users
                    {
                        Username = "ceo",
                        Email = "ceo@example.com",
                        PasswordHash = HashPassword("CEO123!"),
                        Role = "Executive",
                        FirstName = "John",
                        LastName = "Doe",
                        DepartmentId = 1, // Finance
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    },
                    new Users
                    {
                        Username = "executive",
                        Email = "executive@example.com",
                        PasswordHash = HashPassword("Executive123!"),
                        Role = "Executive",
                        FirstName = "Jane",
                        LastName = "Smith",
                        DepartmentId = 1, // Finance
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    },
                    
                    // Department Leads
                    new Users
                    {
                        Username = "finance-lead",
                        Email = "finance-lead@example.com",
                        PasswordHash = HashPassword("FinanceLead123!"),
                        Role = "DepartmentLead",
                        FirstName = "Michael",
                        LastName = "Johnson",
                        DepartmentId = 1, // Finance
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    },
                    new Users
                    {
                        Username = "hr-lead",
                        Email = "hr-lead@example.com",
                        PasswordHash = HashPassword("HRLead123!"),
                        Role = "DepartmentLead",
                        FirstName = "Sarah",
                        LastName = "Williams",
                        DepartmentId = 2, // HR
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    },
                    new Users
                    {
                        Username = "operations-lead",
                        Email = "operations-lead@example.com",
                        PasswordHash = HashPassword("OpsLead123!"),
                        Role = "DepartmentLead",
                        FirstName = "David",
                        LastName = "Brown",
                        DepartmentId = 3, // Operations
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    },
                    new Users
                    {
                        Username = "compliance-lead",
                        Email = "compliance-lead@example.com",
                        PasswordHash = HashPassword("CompLead123!"),
                        Role = "DepartmentLead",
                        FirstName = "Emma",
                        LastName = "Davis",
                        DepartmentId = 4, // Compliance
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    },
                    new Users
                    {
                        Username = "it-lead",
                        Email = "it-lead@example.com",
                        PasswordHash = HashPassword("ITLead123!"),
                        Role = "DepartmentLead",
                        FirstName = "Robert",
                        LastName = "Wilson",
                        DepartmentId = 5, // IT
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    },

                    // Staff Members (one for each department)
                    new Users
                    {
                        Username = "finance-staff",
                        Email = "finance-staff@example.com",
                        PasswordHash = HashPassword("Finance123!"),
                        Role = "Staff",
                        FirstName = "Tom",
                        LastName = "Anderson",
                        DepartmentId = 1,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    },
                    new Users
                    {
                        Username = "hr-staff",
                        Email = "hr-staff@example.com",
                        PasswordHash = HashPassword("HR123!"),
                        Role = "Staff",
                        FirstName = "Lisa",
                        LastName = "Taylor",
                        DepartmentId = 2,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    },
                    new Users
                    {
                        Username = "operations-staff",
                        Email = "operations-staff@example.com",
                        PasswordHash = HashPassword("Ops123!"),
                        Role = "Staff",
                        FirstName = "Mark",
                        LastName = "Miller",
                        DepartmentId = 3,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    },
                    new Users
                    {
                        Username = "compliance-staff",
                        Email = "compliance-staff@example.com",
                        PasswordHash = HashPassword("Comp123!"),
                        Role = "Staff",
                        FirstName = "Anna",
                        LastName = "White",
                        DepartmentId = 4,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    },
                    new Users
                    {
                        Username = "it-staff",
                        Email = "it-staff@example.com",
                        PasswordHash = HashPassword("IT123!"),
                        Role = "Staff",
                        FirstName = "James",
                        LastName = "Lee",
                        DepartmentId = 5,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    }
                };

                await _context.Users.AddRangeAsync(users);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "All users seeded successfully",
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
    }
}
