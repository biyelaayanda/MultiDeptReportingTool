using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Services;

namespace MultiDeptReportingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(loginDto);
            
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid credentials or role mismatch" });
            }

            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _authService.RegisterAsync(registerDto);
            
            if (user == null)
            {
                return BadRequest(new { message = "User already exists" });
            }

            return Ok(new { message = "User registered successfully", userId = user.Id });
        }

        [HttpGet("profile")]
        [Authorize]
        public IActionResult GetProfile()
        {
            var username = User.Identity?.Name;
            var role = User.FindFirst("role")?.Value;
            var department = User.FindFirst("Department")?.Value;
            var email = User.FindFirst("email")?.Value;

            return Ok(new
            {
                username,
                role,
                department,
                email
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // In a real application, you might want to blacklist the token
            return Ok(new { message = "Logged out successfully" });
        }
    }
}