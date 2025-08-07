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
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserNotifications()
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

                // For now, create sample notifications based on user's reports
                var recentReports = await _context.Reports
                    .Where(r => r.CreatedByUserId == userId && r.CreatedAt >= oneWeekAgo)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .Select(r => new
                    {
                        id = Guid.NewGuid().ToString(),
                        title = GetNotificationTitle(r.Status),
                        message = GetNotificationMessage(r.Title, r.Status),
                        type = GetNotificationType(r.Status),
                        timestamp = r.CreatedAt,
                        isRead = false,
                        reportId = r.Id,
                        reportTitle = r.Title
                    })
                    .ToListAsync();

                // Add some system notifications
                var systemNotifications = new List<object>
                {
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        title = "System Maintenance",
                        message = "Scheduled maintenance on Sunday 2:00 AM - 4:00 AM",
                        type = "info",
                        timestamp = now.AddHours(-12),
                        isRead = false,
                        reportId = (int?)null,
                        reportTitle = (string?)null
                    },
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        title = "New Feature Available",
                        message = "Enhanced reporting dashboard is now available",
                        type = "success",
                        timestamp = now.AddDays(-2),
                        isRead = true,
                        reportId = (int?)null,
                        reportTitle = (string?)null
                    }
                };

                var allNotifications = recentReports.Cast<object>()
                    .Concat(systemNotifications)
                    .OrderByDescending(n => ((dynamic)n).timestamp)
                    .Take(15)
                    .ToList();

                return Ok(allNotifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving notifications", error = ex.Message });
            }
        }

        [HttpPost("{notificationId}/mark-read")]
        public async Task<IActionResult> MarkNotificationAsRead(string notificationId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int _))
                {
                    return Unauthorized("Invalid user token");
                }

                // In a real implementation, you'd update the notification in the database
                // For now, just return success
                return Ok(new { message = "Notification marked as read", notificationId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error marking notification as read", error = ex.Message });
            }
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int _))
                {
                    return Unauthorized("Invalid user token");
                }

                // In a real implementation, you'd update all notifications for the user
                // For now, just return success
                return Ok(new { message = "All notifications marked as read" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error marking all notifications as read", error = ex.Message });
            }
        }

        private static string GetNotificationTitle(string? status)
        {
            return status switch
            {
                "Approved" => "Report Approved",
                "UnderReview" => "Report Under Review",
                "Submitted" => "Report Submitted",
                "Rejected" => "Report Needs Revision",
                _ => "Report Status Update"
            };
        }

        private static string GetNotificationMessage(string? reportTitle, string? status)
        {
            var title = reportTitle ?? "Your report";
            return status switch
            {
                "Approved" => $"'{title}' has been approved and is now complete",
                "UnderReview" => $"'{title}' is currently under review",
                "Submitted" => $"'{title}' has been successfully submitted",
                "Rejected" => $"'{title}' requires revisions. Please check feedback",
                _ => $"'{title}' status has been updated"
            };
        }

        private static string GetNotificationType(string? status)
        {
            return status switch
            {
                "Approved" => "success",
                "UnderReview" => "info",
                "Submitted" => "info",
                "Rejected" => "warning",
                _ => "info"
            };
        }
    }
}
