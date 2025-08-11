using System.Text;
using MultiDeptReportingTool.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MultiDeptReportingTool.Middleware
{
    public class ApiSecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiSecurityMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public ApiSecurityMiddleware(
            RequestDelegate next,
            ILogger<ApiSecurityMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context, IApiSecurityService apiSecurityService)
        {
            try
            {
                // Skip security checks for certain paths
                if (ShouldSkipSecurityCheck(context.Request.Path))
                {
                    await _next(context);
                    return;
                }

                var clientId = GetClientId(context);

                // Check basic request validation
                if (!await apiSecurityService.ValidateRequestAsync(context.Request))
                {
                    await WriteErrorResponse(context, 400, "Invalid request format");
                    return;
                }

                // Check rate limiting
                var endpoint = GetEndpointKey(context.Request);
                if (await apiSecurityService.IsRequestThrottledAsync(clientId, endpoint))
                {
                    await WriteErrorResponse(context, 429, "Rate limit exceeded");
                    return;
                }

                // Validate request signature for sensitive endpoints
                if (RequiresSignatureValidation(context.Request.Path))
                {
                    if (!await ValidateRequestSignature(context, apiSecurityService))
                    {
                        await WriteErrorResponse(context, 401, "Invalid request signature");
                        return;
                    }
                }

                // Record API call before processing
                var startTime = DateTime.UtcNow;
                var originalBodyStream = context.Response.Body;

                try
                {
                    using var responseBody = new MemoryStream();
                    context.Response.Body = responseBody;

                    await _next(context);

                    // Record successful API call
                    var isSuccessful = context.Response.StatusCode < 400;
                    await apiSecurityService.RecordApiCallAsync(endpoint, clientId, isSuccessful);

                    // Copy response back to original stream
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
                finally
                {
                    context.Response.Body = originalBodyStream;
                }

                // Log API call metrics
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("API call: {Method} {Path} - {StatusCode} - {Duration}ms - Client: {ClientId}",
                    context.Request.Method, context.Request.Path, context.Response.StatusCode, 
                    duration.TotalMilliseconds, clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Security middleware error");
                await WriteErrorResponse(context, 500, "Internal security error");
            }
        }

        private bool ShouldSkipSecurityCheck(PathString path)
        {
            var skipPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/register",
                "/health",
                "/swagger",
                "/api/security/debug"
            };

            return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
        }

        private string GetClientId(HttpContext context)
        {
            // Try to get client ID from various sources
            if (context.Request.Headers.TryGetValue("X-Client-ID", out var clientId))
            {
                return clientId.FirstOrDefault() ?? "unknown";
            }

            // Use user ID if authenticated
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                return context.User.Identity.Name ?? "authenticated_user";
            }

            // Fall back to IP address
            return GetClientIpAddress(context);
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                return xForwardedFor.Split(',').First().Trim();
            }

            var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xRealIp))
            {
                return xRealIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private string GetEndpointKey(HttpRequest request)
        {
            // Normalize endpoint path for rate limiting
            var path = request.Path.Value?.ToLowerInvariant() ?? "";
            
            // Remove dynamic segments for grouping
            var segments = path.Split('/');
            var normalizedSegments = new List<string>();

            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment))
                    continue;

                // Replace numeric IDs with placeholder
                if (int.TryParse(segment, out _) || Guid.TryParse(segment, out _))
                {
                    normalizedSegments.Add("{id}");
                }
                else
                {
                    normalizedSegments.Add(segment);
                }
            }

            return "/" + string.Join("/", normalizedSegments);
        }

        private bool RequiresSignatureValidation(PathString path)
        {
            var sensitiveEndpoints = new[]
            {
                "/api/export",
                "/api/admin",
                "/api/security",
                "/api/users"
            };

            return sensitiveEndpoints.Any(endpoint => 
                path.StartsWithSegments(endpoint, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<bool> ValidateRequestSignature(HttpContext context, IApiSecurityService apiSecurityService)
        {
            try
            {
                if (!context.Request.Headers.TryGetValue("X-Signature", out var signature) ||
                    !context.Request.Headers.TryGetValue("X-Timestamp", out var timestamp))
                {
                    return false;
                }

                var requestBody = await ReadRequestBodyAsync(context.Request);
                var method = context.Request.Method;
                var path = context.Request.Path.Value ?? "";

                return await apiSecurityService.ValidateRequestSignatureAsync(
                    signature.FirstOrDefault() ?? "",
                    requestBody,
                    timestamp.FirstOrDefault() ?? "",
                    method,
                    path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate request signature");
                return false;
            }
        }

        private async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            if (request.ContentLength == 0 || request.ContentLength == null)
                return string.Empty;

            // Enable buffering to allow multiple reads
            request.EnableBuffering();

            // Read the request body
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            // Reset the stream position for the next middleware
            request.Body.Position = 0;

            return body;
        }

        private async Task WriteErrorResponse(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = message,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                path = context.Request.Path.Value,
                method = context.Request.Method
            };

            var json = System.Text.Json.JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }
}
