using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using System.Security.Claims;

namespace MultiDeptReportingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetDepartments()
        {
            try
            {
                var departments = _context.Departments.ToList();
                return Ok(new { 
                    success = true, 
                    count = departments.Count, 
                    data = departments,
                    message = $"Successfully connected to SQL Server: {Environment.MachineName}\\LNG-F1SY334"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    success = false, 
                    error = ex.Message,
                    message = "Database connection failed"
                });
            }
        }

        [HttpGet("goals")]
        [Authorize]
        public async Task<IActionResult> GetDepartmentGoals()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                // Get user's department
                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.DepartmentId, u.Department.Name })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Sample department goals - in a real app, you'd have a Goals table
                var goals = new List<object>
                {
                    new
                    {
                        id = 1,
                        title = "Monthly Report Completion",
                        description = "Complete all monthly reports on time",
                        target = 100,
                        current = 85,
                        unit = "%",
                        deadline = DateTime.UtcNow.AddDays(15),
                        priority = "High",
                        status = "In Progress",
                        departmentId = user.DepartmentId,
                        departmentName = user.Name
                    },
                    new
                    {
                        id = 2,
                        title = "Quality Score Improvement",
                        description = "Improve report quality scores",
                        target = 95,
                        current = 88,
                        unit = "%",
                        deadline = DateTime.UtcNow.AddDays(30),
                        priority = "Medium",
                        status = "In Progress",
                        departmentId = user.DepartmentId,
                        departmentName = user.Name
                    },
                    new
                    {
                        id = 3,
                        title = "Process Efficiency",
                        description = "Reduce report processing time",
                        target = 48,
                        current = 72,
                        unit = "hours",
                        deadline = DateTime.UtcNow.AddDays(45),
                        priority = "Low",
                        status = "Pending",
                        departmentId = user.DepartmentId,
                        departmentName = user.Name
                    }
                };

                return Ok(goals);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving department goals", error = ex.Message });
            }
        }
    }
}
