using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Services;
using System.Security.Claims;

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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.Identity?.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var department = User.FindFirst("DepartmentId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return BadRequest(new { message = "Invalid user ID in token" });
            }

            return Ok(new
            {
                id = userId,
                username,
                firstName = username, // You might want to add separate firstName/lastName fields to your User model
                lastName = "Member",
                role,
                department,
                email,
                joinDate = DateTime.UtcNow // You might want to add this field to your User model
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Get the refresh token from the request
            var refreshToken = Request.Cookies["refreshToken"];
            
            // If no refresh token, still return success to client
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Ok(new { message = "Logged out successfully" });
            }
            
            // Revoke the token
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "::1";
            await _authService.RevokeTokenAsync(refreshToken, ipAddress, "User logout");
            
            // Remove refresh token cookie
            Response.Cookies.Delete("refreshToken");
            
            return Ok(new { message = "Logged out successfully" });
        }
        
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            // Get the refresh token from the request or cookie
            var refreshToken = request.RefreshToken;
            if (string.IsNullOrEmpty(refreshToken))
            {
                refreshToken = Request.Cookies["refreshToken"];
            }
            
            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest(new { message = "Refresh token is required" });
            }
            
            // Get client's IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "::1";
            
            // Refresh the token
            var response = await _authService.RefreshTokenAsync(refreshToken, ipAddress);
            if (response == null)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }
            
            // Set refresh token in cookie
            SetRefreshTokenCookie(response.RefreshToken);
            
            return Ok(response);
        }
        
        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
        {
            // Get the refresh token from the request or cookie
            var refreshToken = request.RefreshToken;
            if (string.IsNullOrEmpty(refreshToken))
            {
                refreshToken = Request.Cookies["refreshToken"];
            }
            
            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest(new { message = "Refresh token is required" });
            }
            
            // Get client's IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "::1";
            
            // Revoke the token
            var result = await _authService.RevokeTokenAsync(refreshToken, ipAddress, "Revoked by user");
            if (!result)
            {
                return BadRequest(new { message = "Token not found or already revoked" });
            }
            
            return Ok(new { message = "Token revoked successfully" });
        }
        
        private void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Cannot be accessed by client-side script
                Secure = true,   // Only sent over HTTPS
                SameSite = SameSiteMode.Strict, // Prevents CSRF
                Expires = DateTime.UtcNow.AddDays(7) // 7 days expiry
            };
            
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}