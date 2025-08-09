using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MultiDeptReportingTool.Services.Interfaces;
using System.Security.Claims;

namespace MultiDeptReportingTool.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _permission;
        private readonly bool _requireDepartmentMatch;

        public RequirePermissionAttribute(string permission, bool requireDepartmentMatch = false)
        {
            _permission = permission;
            _requireDepartmentMatch = requireDepartmentMatch;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var auditService = context.HttpContext.RequestServices.GetRequiredService<IAuditService>();

            int? departmentId = null;
            if (_requireDepartmentMatch)
            {
                var departmentIdClaim = user.FindFirst("DepartmentId")?.Value;
                if (int.TryParse(departmentIdClaim, out int deptId))
                {
                    departmentId = deptId;
                }
            }

            // Check permission asynchronously
            var hasPermission = Task.Run(async () => 
                await permissionService.HasPermissionAsync(userId, _permission, departmentId)).Result;

            if (!hasPermission)
            {
                // Log unauthorized access attempt
                var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                var username = user.Identity?.Name ?? "";
                
                Task.Run(async () => await auditService.LogSecurityEventAsync(
                    "Access Denied",
                    _permission,
                    userId,
                    username,
                    false,
                    $"User lacks required permission: {_permission}",
                    $"Attempted to access resource requiring permission: {_permission}",
                    ipAddress,
                    context.HttpContext.Request.Headers.UserAgent,
                    departmentId,
                    severity: "Warning"
                ));

                context.Result = new ForbidResult();
                return;
            }

            // Log successful access
            var successIpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var successUsername = user.Identity?.Name ?? "";
            
            Task.Run(async () => await auditService.LogSecurityEventAsync(
                "Permission Check",
                _permission,
                userId,
                successUsername,
                true,
                details: $"Successfully validated permission: {_permission}",
                ipAddress: successIpAddress,
                departmentId: departmentId,
                severity: "Info"
            ));
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public RequireRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userRole) || !_roles.Contains(userRole))
            {
                var auditService = context.HttpContext.RequestServices.GetRequiredService<IAuditService>();
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                var username = user.Identity?.Name ?? "";

                if (int.TryParse(userIdClaim, out int userId))
                {
                    Task.Run(async () => await auditService.LogSecurityEventAsync(
                        "Role Check Failed",
                        "Role Authorization",
                        userId,
                        username,
                        false,
                        $"User role '{userRole}' not in required roles: {string.Join(", ", _roles)}",
                        $"Required roles: {string.Join(", ", _roles)}, User role: {userRole}",
                        ipAddress,
                        context.HttpContext.Request.Headers.UserAgent,
                        severity: "Warning"
                    ));
                }

                context.Result = new ForbidResult();
                return;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuditActionAttribute : Attribute, IActionFilter
    {
        private readonly string _action;
        private readonly string _resource;

        public AuditActionAttribute(string action, string resource)
        {
            _action = action;
            _resource = resource;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Action is starting - could log start time if needed
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var user = context.HttpContext.User;
            var auditService = context.HttpContext.RequestServices.GetRequiredService<IAuditService>();
            
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = user.Identity?.Name ?? "";
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var userAgent = context.HttpContext.Request.Headers.UserAgent;
            var departmentIdClaim = user.FindFirst("DepartmentId")?.Value;
            
            int? userId = null;
            int? departmentId = null;
            
            if (int.TryParse(userIdClaim, out int parsedUserId))
                userId = parsedUserId;
                
            if (int.TryParse(departmentIdClaim, out int parsedDeptId))
                departmentId = parsedDeptId;

            bool isSuccess = context.Result is not BadRequestResult && 
                           context.Result is not UnauthorizedResult &&
                           context.Result is not ForbidResult &&
                           context.Exception == null;

            string? failureReason = null;
            if (!isSuccess)
            {
                if (context.Exception != null)
                    failureReason = context.Exception.Message;
                else if (context.Result is BadRequestObjectResult badRequest)
                    failureReason = badRequest.Value?.ToString();
            }

            Task.Run(async () => await auditService.LogSecurityEventAsync(
                _action,
                _resource,
                userId,
                username,
                isSuccess,
                failureReason,
                $"Action: {_action} on Resource: {_resource}",
                ipAddress,
                userAgent,
                departmentId,
                severity: isSuccess ? "Info" : "Warning"
            ));
        }
    }
}
