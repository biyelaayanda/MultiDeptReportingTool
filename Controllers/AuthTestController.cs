using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Services;

namespace MultiDeptReportingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthTestController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthTestController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("test-login")]
        [AllowAnonymous]
        public async Task<IActionResult> TestLogin()
        {
            // Try to login with a test user
            var loginDto = new LoginDto
            {
                Username = "john.doe@company.com",
                Password = "SecurePass123!"
            };

            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "::1";
                var result = await _authService.LoginAsync(loginDto, ipAddress);
                
                if (result == null)
                {
                    return BadRequest(new { 
                        error = "Login failed", 
                        message = "Test user not found or invalid credentials",
                        testUser = loginDto.Username 
                    });
                }

                return Ok(new
                {
                    message = "Test login successful",
                    token = result.Token,
                    username = result.Username,
                    role = result.Role,
                    email = result.Email,
                    expiresAt = result.ExpiresAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    error = "Test login error", 
                    message = ex.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }

        [HttpGet("verify-token")]
        [Authorize]
        public IActionResult VerifyToken()
        {
            return Ok(new
            {
                message = "Token is valid",
                isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                username = User.Identity?.Name,
                claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }

        [HttpGet("headers")]
        [AllowAnonymous]
        public IActionResult GetHeaders()
        {
            return Ok(new
            {
                headers = Request.Headers.Select(h => new { h.Key, Value = h.Value.ToString() }).ToList(),
                authorization = Request.Headers.Authorization.ToString(),
                hasAuth = Request.Headers.ContainsKey("Authorization")
            });
        }
    }
}
