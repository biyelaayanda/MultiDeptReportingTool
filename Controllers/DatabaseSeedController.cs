using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.Services;

namespace MultiDeptReportingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseSeedController : ControllerBase
    {
        private readonly ComprehensiveDataSeedingService _seedingService;
        private readonly ILogger<DatabaseSeedController> _logger;

        public DatabaseSeedController(
            ComprehensiveDataSeedingService seedingService,
            ILogger<DatabaseSeedController> logger)
        {
            _seedingService = seedingService;
            _logger = logger;
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
                _logger.LogError(ex, "Error occurred during comprehensive data seeding");
                return StatusCode(500, new { 
                    message = "Error occurred during data seeding", 
                    error = ex.Message 
                });
            }
        }

        [HttpGet("seeding-status")]
        public IActionResult GetSeedingStatus()
        {
            return Ok(new { 
                message = "Database seeding service is available",
                timestamp = DateTime.UtcNow 
            });
        }
    }
}
