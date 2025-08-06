using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;

namespace MultiDeptReportingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Department)
                    .Where(u => u.IsActive)
                    .Select(u => new {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        u.Role,
                        u.DepartmentId,
                        DepartmentName = u.Department != null ? u.Department.Name : null,
                        u.CreatedAt,
                        u.LastLoginAt,
                        u.IsActive
                    })
                    .ToListAsync();

                return Ok(new { 
                    success = true, 
                    count = users.Count, 
                    data = users 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Department)
                    .Where(u => u.Id == id && u.IsActive)
                    .Select(u => new {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        u.Role,
                        u.DepartmentId,
                        DepartmentName = u.Department != null ? u.Department.Name : null,
                        u.CreatedAt,
                        u.LastLoginAt
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                return Ok(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }
    }
}
