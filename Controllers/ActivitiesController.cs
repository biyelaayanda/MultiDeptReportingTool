using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using System.Security.Claims;

namespace MultiDeptReportingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ActivitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ActivitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserActivities()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var now = DateTime.UtcNow;
                var oneMonthAgo = now.AddMonths(-1);

                // Get recent reports activity
                var reportActivities = await _context.Reports
                    .Where(r => r.CreatedByUserId == userId && r.CreatedAt >= oneMonthAgo)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(20)
                    .Select(r => new
                    {
                        id = Guid.NewGuid().ToString(),
                        type = GetActivityType(r.Status),
                        description = GetActivityDescription(r.Title, r.Status),
                        timestamp = r.CreatedAt,
                        reportId = r.Id,
                        reportTitle = r.Title,
                        departmentName = r.Department != null ? r.Department.Name : "Unknown",
                        status = r.Status,
                        icon = GetActivityIcon(r.Status),
                        color = GetActivityColor(r.Status)
                    })
                    .ToListAsync();

                // Add some system activities
                var systemActivities = new List<object>
                {
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        type = "login",
                        description = "Logged into the system",
                        timestamp = now.AddHours(-2),
                        reportId = (int?)null,
                        reportTitle = (string?)null,
                        departmentName = (string?)null,
                        status = (string?)null,
                        icon = "fas fa-sign-in-alt",
                        color = "text-info"
                    },
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        type = "profile",
                        description = "Viewed profile settings",
                        timestamp = now.AddDays(-1),
                        reportId = (int?)null,
                        reportTitle = (string?)null,
                        departmentName = (string?)null,
                        status = (string?)null,
                        icon = "fas fa-user",
                        color = "text-secondary"
                    },
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        type = "dashboard",
                        description = "Accessed dashboard",
                        timestamp = now.AddDays(-2),
                        reportId = (int?)null,
                        reportTitle = (string?)null,
                        departmentName = (string?)null,
                        status = (string?)null,
                        icon = "fas fa-chart-line",
                        color = "text-primary"
                    }
                };

                var allActivities = reportActivities.Cast<object>()
                    .Concat(systemActivities)
                    .OrderByDescending(a => ((dynamic)a).timestamp)
                    .Take(30)
                    .ToList();

                return Ok(allActivities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving activities", error = ex.Message });
            }
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentActivities(int limit = 10)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var now = DateTime.UtcNow;
                var oneWeekAgo = now.AddDays(-7);

                var activities = await _context.Reports
                    .Where(r => r.CreatedByUserId == userId && r.CreatedAt >= oneWeekAgo)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(limit)
                    .Select(r => new
                    {
                        id = Guid.NewGuid().ToString(),
                        action = GetSimpleActivityAction(r.Status),
                        description = GetSimpleActivityDescription(r.Title, r.Status),
                        timestamp = r.CreatedAt,
                        reportId = r.Id,
                        timeAgo = GetTimeAgo(r.CreatedAt)
                    })
                    .ToListAsync();

                return Ok(activities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving recent activities", error = ex.Message });
            }
        }

        private static string GetActivityType(string? status)
        {
            return status switch
            {
                "Approved" => "approval",
                "UnderReview" => "review",
                "Submitted" => "submission",
                "Rejected" => "revision",
                "Draft" => "creation",
                _ => "update"
            };
        }

        private static string GetActivityDescription(string? reportTitle, string? status)
        {
            var title = reportTitle ?? "Untitled Report";
            return status switch
            {
                "Approved" => $"Report '{title}' was approved",
                "UnderReview" => $"Report '{title}' submitted for review",
                "Submitted" => $"Report '{title}' was submitted",
                "Rejected" => $"Report '{title}' was returned for revision",
                "Draft" => $"Report '{title}' was created",
                _ => $"Report '{title}' was updated"
            };
        }

        private static string GetActivityIcon(string? status)
        {
            return status switch
            {
                "Approved" => "fas fa-check-circle",
                "UnderReview" => "fas fa-clock",
                "Submitted" => "fas fa-paper-plane",
                "Rejected" => "fas fa-exclamation-triangle",
                "Draft" => "fas fa-edit",
                _ => "fas fa-file-alt"
            };
        }

        private static string GetActivityColor(string? status)
        {
            return status switch
            {
                "Approved" => "text-success",
                "UnderReview" => "text-info",
                "Submitted" => "text-primary",
                "Rejected" => "text-warning",
                "Draft" => "text-secondary",
                _ => "text-muted"
            };
        }

        private static string GetSimpleActivityAction(string? status)
        {
            return status switch
            {
                "Approved" => "approved",
                "UnderReview" => "submitted for review",
                "Submitted" => "submitted",
                "Rejected" => "returned",
                "Draft" => "created",
                _ => "updated"
            };
        }

        private static string GetSimpleActivityDescription(string? reportTitle, string? status)
        {
            var title = reportTitle ?? "report";
            var action = GetSimpleActivityAction(status);
            return $"You {action} {title}";
        }

        private static string GetTimeAgo(DateTime timestamp)
        {
            var now = DateTime.UtcNow;
            var timeSpan = now - timestamp;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
            
            return timestamp.ToString("MMM dd, yyyy");
        }
    }
}
