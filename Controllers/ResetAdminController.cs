using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Models;
using MultiDeptReportingTool.Services;
using System.Threading.Tasks;

namespace MultiDeptReportingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResetAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;
        private readonly ILogger<ResetAdminController> _logger;

        public ResetAdminController(
            ApplicationDbContext context,
            IAuthService authService,
            ILogger<ResetAdminController> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin()
        {
            try
            {
                // Check if admin user exists
                var adminUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == "admin");

                // If admin exists, reset password
                if (adminUser != null)
                {
                    var (hash, salt) = await _authService.HashPasswordAsync("Admin123!");
                    
                    adminUser.PasswordHash = hash;
                    adminUser.PasswordSalt = salt;
                    adminUser.IsActive = true;
                    adminUser.Role = "Admin";
                    
                    _context.Users.Update(adminUser);
                    await _context.SaveChangesAsync();
                    
                    return Ok(new { message = "Admin user reset successfully" });
                }
                
                // If admin doesn't exist, create a new one
                var registerDto = new RegisterDto
                {
                    Username = "admin",
                    Email = "admin@example.com",
                    Password = "Admin123!",
                    FirstName = "Admin",
                    LastName = "User",
                    Role = "Admin",
                    DepartmentId = 1  // Assuming Finance department has ID 1
                };
                
                var result = await _authService.RegisterAsync(registerDto);
                
                if (result == null)
                {
                    return BadRequest(new { message = "Failed to create admin user" });
                }
                
                return Ok(new { message = "Admin user created successfully", userId = result.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/resetting admin user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
