using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MultiDeptReportingTool.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public SecurityHeadersMiddleware(
            RequestDelegate next,
            ILogger<SecurityHeadersMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers before processing the request
            AddSecurityHeaders(context);

            await _next(context);

            // Log security header information for debugging
            LogSecurityHeaders(context);
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            var response = context.Response;
            var request = context.Request;

            try
            {
                // X-Content-Type-Options: Prevent MIME type sniffing
                if (!response.Headers.ContainsKey("X-Content-Type-Options"))
                {
                    response.Headers.Add("X-Content-Type-Options", "nosniff");
                }

                // X-Frame-Options: Prevent clickjacking
                if (!response.Headers.ContainsKey("X-Frame-Options"))
                {
                    response.Headers.Add("X-Frame-Options", "DENY");
                }

                // X-XSS-Protection: Enable browser XSS protection
                if (!response.Headers.ContainsKey("X-XSS-Protection"))
                {
                    response.Headers.Add("X-XSS-Protection", "1; mode=block");
                }

                // Strict-Transport-Security: Force HTTPS
                if (request.IsHttps && !response.Headers.ContainsKey("Strict-Transport-Security"))
                {
                    var maxAge = _configuration.GetValue<int>("Security:HSTS:MaxAgeSeconds", 31536000); // 1 year default
                    var includeSubDomains = _configuration.GetValue<bool>("Security:HSTS:IncludeSubDomains", true);
                    var preload = _configuration.GetValue<bool>("Security:HSTS:Preload", false);

                    var hstsValue = $"max-age={maxAge}";
                    if (includeSubDomains) hstsValue += "; includeSubDomains";
                    if (preload) hstsValue += "; preload";

                    response.Headers.Add("Strict-Transport-Security", hstsValue);
                }

                // Content-Security-Policy: Prevent XSS and injection attacks
                if (!response.Headers.ContainsKey("Content-Security-Policy"))
                {
                    var csp = BuildContentSecurityPolicy();
                    response.Headers.Add("Content-Security-Policy", csp);
                }

                // Referrer-Policy: Control referrer information
                if (!response.Headers.ContainsKey("Referrer-Policy"))
                {
                    response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
                }

                // Permissions-Policy: Control browser features
                if (!response.Headers.ContainsKey("Permissions-Policy"))
                {
                    var permissionsPolicy = BuildPermissionsPolicy();
                    response.Headers.Add("Permissions-Policy", permissionsPolicy);
                }

                // X-Permitted-Cross-Domain-Policies: Restrict cross-domain access
                if (!response.Headers.ContainsKey("X-Permitted-Cross-Domain-Policies"))
                {
                    response.Headers.Add("X-Permitted-Cross-Domain-Policies", "none");
                }

                // Cache-Control: Prevent caching of sensitive data
                if (IsSensitiveEndpoint(request.Path) && !response.Headers.ContainsKey("Cache-Control"))
                {
                    response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate, private");
                    response.Headers.Add("Pragma", "no-cache");
                    response.Headers.Add("Expires", "0");
                }

                // Remove server information headers
                response.Headers.Remove("Server");
                response.Headers.Remove("X-Powered-By");
                response.Headers.Remove("X-AspNet-Version");
                response.Headers.Remove("X-AspNetMvc-Version");

                // Add custom security headers
                if (!response.Headers.ContainsKey("X-Security-Timestamp"))
                {
                    response.Headers.Add("X-Security-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                }

                if (!response.Headers.ContainsKey("X-Request-ID"))
                {
                    var requestId = context.TraceIdentifier ?? Guid.NewGuid().ToString();
                    response.Headers.Add("X-Request-ID", requestId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add security headers");
            }
        }

        private string BuildContentSecurityPolicy()
        {
            var allowedOrigins = _configuration.GetSection("Security:CSP:AllowedOrigins").Get<string[]>() 
                ?? new[] { "'self'" };

            var policies = new List<string>
            {
                "default-src 'self'",
                $"script-src 'self' 'unsafe-inline' 'unsafe-eval' {string.Join(" ", allowedOrigins)}",
                $"style-src 'self' 'unsafe-inline' {string.Join(" ", allowedOrigins)}",
                $"img-src 'self' data: https: {string.Join(" ", allowedOrigins)}",
                $"font-src 'self' {string.Join(" ", allowedOrigins)}",
                "connect-src 'self' https:",
                "frame-ancestors 'none'",
                "form-action 'self'",
                "base-uri 'self'",
                "object-src 'none'",
                "media-src 'self'",
                "worker-src 'self'",
                "child-src 'self'",
                "manifest-src 'self'"
            };

            // Add report-uri if configured
            var reportUri = _configuration["Security:CSP:ReportUri"];
            if (!string.IsNullOrEmpty(reportUri))
            {
                policies.Add($"report-uri {reportUri}");
            }

            return string.Join("; ", policies);
        }

        private string BuildPermissionsPolicy()
        {
            var policies = new List<string>
            {
                "accelerometer=()",
                "camera=()",
                "geolocation=()",
                "gyroscope=()",
                "magnetometer=()",
                "microphone=()",
                "payment=()",
                "usb=()",
                "interest-cohort=()"
            };

            return string.Join(", ", policies);
        }

        private bool IsSensitiveEndpoint(PathString path)
        {
            var sensitiveEndpoints = new[]
            {
                "/api/auth",
                "/api/admin",
                "/api/security",
                "/api/export",
                "/api/users",
                "/api/analytics"
            };

            return sensitiveEndpoints.Any(endpoint => 
                path.StartsWithSegments(endpoint, StringComparison.OrdinalIgnoreCase));
        }

        private void LogSecurityHeaders(HttpContext context)
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var securityHeaders = context.Response.Headers
                        .Where(h => IsSecurityHeader(h.Key))
                        .ToDictionary(h => h.Key, h => h.Value.ToString());

                    _logger.LogDebug("Security headers added for {Method} {Path}: {@SecurityHeaders}",
                        context.Request.Method, context.Request.Path, securityHeaders);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security headers");
            }
        }

        private bool IsSecurityHeader(string headerName)
        {
            var securityHeaders = new[]
            {
                "X-Content-Type-Options",
                "X-Frame-Options",
                "X-XSS-Protection",
                "Strict-Transport-Security",
                "Content-Security-Policy",
                "Referrer-Policy",
                "Permissions-Policy",
                "X-Permitted-Cross-Domain-Policies",
                "X-Security-Timestamp",
                "X-Request-ID"
            };

            return securityHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
        }
    }
}
