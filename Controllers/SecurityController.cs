using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.Services;
using MultiDeptReportingTool.Services.Interfaces;
using MultiDeptReportingTool.Attributes;
using MultiDeptReportingTool.Constants;
using MultiDeptReportingTool.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MultiDeptReportingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SecurityController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;
        private readonly ApplicationDbContext _context;

        public SecurityController(IPermissionService permissionService, IAuditService auditService, ApplicationDbContext context)
        {
            _permissionService = permissionService;
            _auditService = auditService;
            _context = context;
        }

        // Permission Management Endpoints
        [HttpGet("permissions")]
        [AllowAnonymous] // Temporarily bypassing permission check for testing
        [AuditAction("View", "Permissions")]
        public async Task<IActionResult> GetAllPermissions()
        {
            var permissions = await _permissionService.GetAllPermissionsAsync();
            return Ok(permissions);
        }

        [HttpGet("roles")]
        [RequirePermission(Permissions.SECURITY_PERMISSIONS_MANAGE)]
        [AuditAction("View", "Roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _permissionService.GetAllRolesAsync();
            return Ok(roles);
        }

        [HttpGet("users/{userId}/permissions")]
        [RequirePermission(Permissions.SECURITY_PERMISSIONS_MANAGE)]
        [AuditAction("View", "UserPermissions")]
        public async Task<IActionResult> GetUserPermissions(int userId, [FromQuery] int? departmentId = null)
        {
            var permissions = await _permissionService.GetUserPermissionsAsync(userId, departmentId);
            return Ok(permissions);
        }

        [HttpPost("users/{userId}/permissions")]
        [RequirePermission(Permissions.SECURITY_PERMISSIONS_MANAGE)]
        [AuditAction("Grant", "UserPermission")]
        public async Task<IActionResult> GrantUserPermission(int userId, [FromBody] GrantPermissionRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            var result = await _permissionService.GrantPermissionAsync(
                userId, request.Permission, currentUserId.Value, request.ExpiresAt);

            if (result)
            {
                return Ok(new { message = "Permission granted successfully" });
            }

            return BadRequest(new { message = "Failed to grant permission" });
        }

        [HttpDelete("users/{userId}/permissions/{permission}")]
        [RequirePermission(Permissions.SECURITY_PERMISSIONS_MANAGE)]
        [AuditAction("Revoke", "UserPermission")]
        public async Task<IActionResult> RevokeUserPermission(int userId, string permission)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            var result = await _permissionService.RevokePermissionAsync(userId, permission, currentUserId.Value);

            if (result)
            {
                return Ok(new { message = "Permission revoked successfully" });
            }

            return BadRequest(new { message = "Failed to revoke permission" });
        }

        [HttpPost("roles/{roleName}/permissions")]
        [RequirePermission(Permissions.SECURITY_PERMISSIONS_MANAGE)]
        [AuditAction("Grant", "RolePermission")]
        public async Task<IActionResult> GrantRolePermission(string roleName, [FromBody] GrantRolePermissionRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            var result = await _permissionService.GrantRolePermissionAsync(roleName, request.Permission, currentUserId.Value);

            if (result)
            {
                return Ok(new { message = "Role permission granted successfully" });
            }

            return BadRequest(new { message = "Failed to grant role permission" });
        }

        [HttpDelete("roles/{roleName}/permissions/{permission}")]
        [RequirePermission(Permissions.SECURITY_PERMISSIONS_MANAGE)]
        [AuditAction("Revoke", "RolePermission")]
        public async Task<IActionResult> RevokeRolePermission(string roleName, string permission)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            var result = await _permissionService.RevokeRolePermissionAsync(roleName, permission, currentUserId.Value);

            if (result)
            {
                return Ok(new { message = "Role permission revoked successfully" });
            }

            return BadRequest(new { message = "Failed to revoke role permission" });
        }

        // Audit and Monitoring Endpoints
        [HttpGet("audit-logs")]
        [RequirePermission(Permissions.SECURITY_AUDIT_VIEW)]
        [AuditAction("View", "AuditLogs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? userId = null,
            [FromQuery] string? action = null,
            [FromQuery] string? ipAddress = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var logs = await _auditService.GetSecurityAuditLogsAsync(
                startDate, endDate, userId, action, ipAddress, pageNumber, pageSize);
            return Ok(logs);
        }

        [HttpGet("alerts")]
        [RequirePermission(Permissions.SECURITY_ALERTS_VIEW)]
        [AuditAction("View", "SecurityAlerts")]
        public async Task<IActionResult> GetSecurityAlerts(
            [FromQuery] bool? isResolved = null,
            [FromQuery] string? severity = null,
            [FromQuery] string? alertType = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var alerts = await _auditService.GetSecurityAlertsAsync(
                isResolved, severity, alertType, pageNumber, pageSize);
            return Ok(alerts);
        }

        [HttpPost("alerts/{alertId}/resolve")]
        [RequirePermission(Permissions.SECURITY_ALERTS_MANAGE)]
        [AuditAction("Resolve", "SecurityAlert")]
        public async Task<IActionResult> ResolveSecurityAlert(int alertId, [FromBody] ResolveAlertRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            var result = await _auditService.ResolveSecurityAlertAsync(alertId, currentUserId.Value, request.ResolutionNotes);

            if (result)
            {
                return Ok(new { message = "Alert resolved successfully" });
            }

            return BadRequest(new { message = "Failed to resolve alert" });
        }

        [HttpGet("system-events")]
        [RequirePermission(Permissions.SYSTEM_AUDIT_VIEW)]
        [AuditAction("View", "SystemEvents")]
        public async Task<IActionResult> GetSystemEvents(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? eventType = null,
            [FromQuery] string? level = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var events = await _auditService.GetSystemEventsAsync(
                startDate, endDate, eventType, level, pageNumber, pageSize);
            return Ok(events);
        }

        [HttpGet("dashboard")]
        [RequirePermission(Permissions.SECURITY_AUDIT_VIEW)]
        [AuditAction("View", "SecurityDashboard")]
        public async Task<IActionResult> GetSecurityDashboard()
        {
            var dashboardData = await _auditService.GetSecurityDashboardDataAsync();
            return Ok(dashboardData);
        }

        [HttpPost("permissions")]
        [RequirePermission(Permissions.SECURITY_PERMISSIONS_MANAGE)]
        [AuditAction("Create", "Permission")]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
        {
            var result = await _permissionService.CreatePermissionAsync(
                request.Name, request.Resource, request.Action, request.Description);

            if (result)
            {
                return Ok(new { message = "Permission created successfully" });
            }

            return BadRequest(new { message = "Failed to create permission or permission already exists" });
        }

        [HttpPost("roles")]
        [RequirePermission(Permissions.SECURITY_PERMISSIONS_MANAGE)]
        [AuditAction("Create", "Role")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            var result = await _permissionService.CreateRoleAsync(request.Name, request.Description);

            if (result)
            {
                return Ok(new { message = "Role created successfully" });
            }

            return BadRequest(new { message = "Failed to create role or role already exists" });
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }

        // Security Seeding Endpoint
        [HttpPost("seed")]
        [AllowAnonymous]
        public async Task<IActionResult> SeedSecurityData([FromServices] SecuritySeedingService seedingService)
        {
            try
            {
                await seedingService.SeedSecurityDataAsync();
                return Ok(new { message = "Security data seeded successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to seed security data", error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        // Link Users to Roles Endpoint
        [HttpPost("link-users-roles")]
        [AllowAnonymous]
        public async Task<IActionResult> LinkUsersToRoles([FromServices] SecuritySeedingService seedingService)
        {
            try
            {
                await seedingService.LinkUsersToRolesAsync();
                return Ok(new { message = "Users linked to roles successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to link users to roles", error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        // Seed Permissions Only Endpoint
        [HttpPost("seed-permissions")]
        [AllowAnonymous]
        public async Task<IActionResult> SeedPermissions([FromServices] SecuritySeedingService seedingService)
        {
            try
            {
                await seedingService.SeedPermissionsAsync();
                return Ok(new { message = "Permissions seeded successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to seed permissions", error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        // Seed Role Permissions Only Endpoint
        [HttpPost("seed-role-permissions")]
        [AllowAnonymous]
        public async Task<IActionResult> SeedRolePermissions([FromServices] SecuritySeedingService seedingService)
        {
            try
            {
                await seedingService.SeedRolePermissionsAsync();
                return Ok(new { message = "Role permissions seeded successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to seed role permissions", error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        // Debug database state endpoint
        [HttpGet("debug/database")]
        [AllowAnonymous]
        public async Task<IActionResult> DebugDatabase()
        {
            try
            {
                var permissions = await _context.Permissions.ToListAsync();
                var roles = await _context.Roles.ToListAsync();
                var rolePermissions = await _context.RolePermissions
                    .Include(rp => rp.Role)
                    .Include(rp => rp.Permission)
                    .ToListAsync();
                
                return Ok(new { 
                    permissionsCount = permissions.Count,
                    permissions = permissions.Select(p => new { p.Id, p.Name }).ToList(),
                    rolesCount = roles.Count,
                    roles = roles.Select(r => new { r.Id, r.Name }).ToList(),
                    rolePermissionsCount = rolePermissions.Count,
                    rolePermissions = rolePermissions.Select(rp => new { 
                        rp.RoleId, 
                        RoleName = rp.Role.Name, 
                        rp.PermissionId, 
                        PermissionName = rp.Permission.Name,
                        rp.GrantedAt
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return Ok(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
        [HttpGet("debug/permissions")]
        [AllowAnonymous]
        public async Task<IActionResult> DebugPermissions()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Ok(new { error = "No valid user ID in token", userId = userIdClaim });
            }

            try
            {
                var hasPermission = await _permissionService.HasPermissionAsync(userId, Permissions.SECURITY_PERMISSIONS_MANAGE);
                var userPermissions = await _permissionService.GetUserPermissionsAsync(userId);
                
                // Get detailed user info with role relationships
                var user = await _context.Users
                    .Include(u => u.RoleEntity)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                
                // Get all roles and their permissions for debugging
                var roles = await _context.Roles
                    .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .ToListAsync();
                
                return Ok(new { 
                    userId = userId,
                    hasSecurityPermissionsManage = hasPermission,
                    allUserPermissions = userPermissions,
                    isAuthenticated = User.Identity?.IsAuthenticated,
                    userName = User.Identity?.Name,
                    claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                    userDetails = user == null ? null : new {
                        user.Id,
                        user.Username,
                        user.Role, // legacy string role
                        user.RoleId, // new foreign key
                        RoleName = user.RoleEntity?.Name,
                        user.IsActive
                    },
                    allRolesWithPermissions = roles.Select(r => new {
                        r.Id,
                        r.Name,
                        Permissions = r.RolePermissions.Select(rp => rp.Permission.Name).ToList()
                    })
                });
            }
            catch (Exception ex)
            {
                return Ok(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }

    // DTOs for Security Controller
    public class GrantPermissionRequest
    {
        public string Permission { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
    }

    public class GrantRolePermissionRequest
    {
        public string Permission { get; set; } = string.Empty;
    }

    public class ResolveAlertRequest
    {
        public string? ResolutionNotes { get; set; }
    }

    public class CreatePermissionRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
