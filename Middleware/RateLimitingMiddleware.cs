using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MultiDeptReportingTool.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly int _maxRequestsPerMinute;
        private readonly int _maxFailedLoginAttempts;
        private readonly int _lockoutMinutes;

        // Thread-safe dictionaries to track requests and failed login attempts
        private static readonly ConcurrentDictionary<string, RateLimitInfo> _requestCounts = new();
        private static readonly ConcurrentDictionary<string, FailedLoginInfo> _failedLogins = new();

        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _maxRequestsPerMinute = configuration.GetValue<int>("Security:RateLimiting:MaxRequestsPerMinute", 60);
            _maxFailedLoginAttempts = configuration.GetValue<int>("Security:RateLimiting:MaxFailedLoginAttempts", 5);
            _lockoutMinutes = configuration.GetValue<int>("Security:RateLimiting:LockoutMinutes", 15);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = GetClientIpAddress(context);
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            var endpoint = $"{ipAddress}_{path}";
            var isLoginRequest = path.Contains("/auth/login") && 
                                 context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase);

            // Check if this IP is locked out for login attempts
            if (isLoginRequest && IsLockedOut(ipAddress))
            {
                _logger.LogWarning("IP {IpAddress} is locked out due to failed login attempts", ipAddress);
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Too many failed login attempts. Please try again later.",
                    lockedUntil = _failedLogins[ipAddress].LockedUntil
                });
                return;
            }

            // Check rate limit for all requests
            if (!CheckRateLimit(endpoint))
            {
                _logger.LogWarning("Rate limit exceeded for endpoint {Endpoint}", endpoint);
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded. Please try again later." });
                return;
            }

            // If we get here, request can proceed
            var originalBodyStream = context.Response.Body;
            
            try
            {
                // For login endpoints, we need to check for failed attempts
                if (isLoginRequest)
                {
                    // Capture the response to check for authentication failures
                    await using var responseBody = new MemoryStream();
                    context.Response.Body = responseBody;

                    await _next(context);

                    // If status code indicates failure, record failed attempt
                    if (context.Response.StatusCode == 401 || context.Response.StatusCode == 403)
                    {
                        RecordFailedLoginAttempt(ipAddress);
                    }
                    else
                    {
                        // Reset failed login attempts on successful login
                        ResetFailedLoginAttempts(ipAddress);
                    }

                    // Copy the response to the original stream
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
                else
                {
                    await _next(context);
                }
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private bool CheckRateLimit(string endpoint)
        {
            var now = DateTime.UtcNow;
            var info = _requestCounts.GetOrAdd(endpoint, _ => new RateLimitInfo { Timestamp = now });

            // Reset counter if minute window has passed
            if ((now - info.Timestamp).TotalMinutes >= 1)
            {
                info.Count = 0;
                info.Timestamp = now;
            }

            // Increment and check
            if (Interlocked.Increment(ref info.Count) > _maxRequestsPerMinute)
            {
                return false;
            }

            return true;
        }

        private bool IsLockedOut(string ipAddress)
        {
            if (_failedLogins.TryGetValue(ipAddress, out var info))
            {
                if (info.LockedUntil > DateTime.UtcNow)
                {
                    return true;
                }
            }
            return false;
        }

        private void RecordFailedLoginAttempt(string ipAddress)
        {
            var now = DateTime.UtcNow;
            var info = _failedLogins.GetOrAdd(ipAddress, _ => new FailedLoginInfo());

            // Reset counter if window has passed
            if ((now - info.LastFailedAttempt).TotalMinutes >= _lockoutMinutes)
            {
                info.Count = 0;
            }

            // Increment and check
            info.LastFailedAttempt = now;
            if (Interlocked.Increment(ref info.Count) >= _maxFailedLoginAttempts)
            {
                info.LockedUntil = now.AddMinutes(_lockoutMinutes);
                _logger.LogWarning("IP {IpAddress} has been locked out until {LockoutTime} due to failed login attempts", 
                    ipAddress, info.LockedUntil);
            }
        }

        private void ResetFailedLoginAttempts(string ipAddress)
        {
            if (_failedLogins.TryGetValue(ipAddress, out var info))
            {
                info.Count = 0;
                info.LockedUntil = DateTime.MinValue;
            }
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Try to get X-Forwarded-For header
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // The first IP in the list is the client's IP
                return forwardedFor.Split(',')[0].Trim();
            }

            // Fallback to connection remote IP address
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private class RateLimitInfo
        {
            public int Count;
            public DateTime Timestamp;
        }

        private class FailedLoginInfo
        {
            public int Count;
            public DateTime LastFailedAttempt = DateTime.UtcNow;
            public DateTime LockedUntil = DateTime.MinValue;
        }
    }

    // Extension method to make it easier to add the middleware
    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}
