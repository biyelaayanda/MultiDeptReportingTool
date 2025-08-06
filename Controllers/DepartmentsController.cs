using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.Data;

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
    }
}
