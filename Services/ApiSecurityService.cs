using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using MultiDeptReportingTool.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MultiDeptReportingTool.Services
{
    public class ApiSecurityService : IApiSecurityService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiSecurityService> _logger;
        private readonly string _apiSecret;
        private readonly ConcurrentDictionary<string, ApiCallRecord> _apiCallHistory;
        private readonly ConcurrentDictionary<string, ThrottleInfo> _throttleData;
        private readonly List<string> _supportedVersions;

        public ApiSecurityService(IConfiguration configuration, ILogger<ApiSecurityService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _apiSecret = _configuration["Security:ApiSecurity:Secret"] ?? "DefaultApiSecretKey_ChangeMeInProduction!";
            _apiCallHistory = new ConcurrentDictionary<string, ApiCallRecord>();
            _throttleData = new ConcurrentDictionary<string, ThrottleInfo>();
            _supportedVersions = new List<string> { "1.0", "1.1" }; // Add supported API versions
        }

        public async Task<string> SignRequestAsync(string requestBody, string timestamp, string method, string path)
        {
            try
            {
                var stringToSign = $"{method.ToUpper()}\n{path}\n{timestamp}\n{requestBody}";
                var secretBytes = Encoding.UTF8.GetBytes(_apiSecret);
                
                using var hmac = new HMACSHA256(secretBytes);
                var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
                
                return Convert.ToBase64String(signatureBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sign request");
                throw new InvalidOperationException("Request signing failed", ex);
            }
        }

        public async Task<bool> ValidateRequestSignatureAsync(string signature, string requestBody, string timestamp, string method, string path)
        {
            try
            {
                // Check timestamp freshness (5 minutes window)
                if (!DateTime.TryParse(timestamp, out var requestTime))
                    return false;
                
                var timeDiff = Math.Abs((DateTime.UtcNow - requestTime).TotalMinutes);
                if (timeDiff > 5)
                {
                    _logger.LogWarning("Request timestamp too old: {TimeDiff} minutes", timeDiff);
                    return false;
                }

                var expectedSignature = await SignRequestAsync(requestBody, timestamp, method, path);
                var isValid = signature == expectedSignature;
                
                if (!isValid)
                {
                    _logger.LogWarning("Invalid request signature for {Method} {Path}", method, path);
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate request signature");
                return false;
            }
        }

        public async Task<bool> ValidateRequestAsync(HttpRequest request)
        {
            try
            {
                // Check for required security headers
                if (!request.Headers.ContainsKey("X-API-Version"))
                {
                    _logger.LogWarning("Missing X-API-Version header");
                    return false;
                }

                var version = request.Headers["X-API-Version"].FirstOrDefault();
                if (!IsVersionSupported(version))
                {
                    _logger.LogWarning("Unsupported API version: {Version}", version);
                    return false;
                }

                // Check Content-Type for POST/PUT requests
                if (request.Method == "POST" || request.Method == "PUT")
                {
                    var contentType = request.ContentType;
                    if (string.IsNullOrEmpty(contentType) || 
                        (!contentType.Contains("application/json") && !contentType.Contains("multipart/form-data")))
                    {
                        _logger.LogWarning("Invalid or missing Content-Type for {Method} request", request.Method);
                        return false;
                    }
                }

                // Validate request size
                var maxRequestSize = _configuration.GetValue<long>("Security:ApiSecurity:MaxRequestSizeBytes", 10485760); // 10MB default
                if (request.ContentLength > maxRequestSize)
                {
                    _logger.LogWarning("Request size exceeds limit: {Size} bytes", request.ContentLength);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate request");
                return false;
            }
        }

        public async Task<bool> IsRequestThrottledAsync(string clientId, string endpoint)
        {
            try
            {
                var key = $"{clientId}_{endpoint}";
                var now = DateTime.UtcNow;
                
                if (!_throttleData.TryGetValue(key, out var throttleInfo))
                {
                    _throttleData[key] = new ThrottleInfo
                    {
                        RequestCount = 1,
                        WindowStart = now,
                        LastRequest = now
                    };
                    return false;
                }

                // Reset window if it's been more than 1 minute
                if ((now - throttleInfo.WindowStart).TotalMinutes >= 1)
                {
                    throttleInfo.RequestCount = 1;
                    throttleInfo.WindowStart = now;
                    throttleInfo.LastRequest = now;
                    return false;
                }

                // Check rate limits based on endpoint type
                var maxRequests = GetMaxRequestsForEndpoint(endpoint);
                throttleInfo.RequestCount++;
                throttleInfo.LastRequest = now;

                if (throttleInfo.RequestCount > maxRequests)
                {
                    _logger.LogWarning("Request throttled for client {ClientId} on endpoint {Endpoint}. Count: {Count}/{Max}", 
                        clientId, endpoint, throttleInfo.RequestCount, maxRequests);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check request throttling");
                return false; // Allow request if check fails
            }
        }

        public string GetRequestVersion(HttpRequest request)
        {
            if (request.Headers.TryGetValue("X-API-Version", out var version))
            {
                return version.FirstOrDefault() ?? "1.0";
            }
            
            // Check query parameter as fallback
            if (request.Query.TryGetValue("version", out var queryVersion))
            {
                return queryVersion.FirstOrDefault() ?? "1.0";
            }
            
            return "1.0"; // Default version
        }

        public bool IsVersionSupported(string? version)
        {
            if (string.IsNullOrEmpty(version))
                return false;
                
            return _supportedVersions.Contains(version);
        }

        public async Task<bool> CheckEndpointRateLimitAsync(string endpoint, string clientId)
        {
            return !(await IsRequestThrottledAsync(clientId, endpoint));
        }

        public async Task RecordApiCallAsync(string endpoint, string clientId, bool successful)
        {
            try
            {
                var key = $"{clientId}_{endpoint}_{DateTime.UtcNow:yyyyMMddHH}"; // Hourly buckets
                
                _apiCallHistory.AddOrUpdate(key, 
                    new ApiCallRecord 
                    { 
                        Endpoint = endpoint, 
                        ClientId = clientId, 
                        Successful = successful ? 1 : 0,
                        Failed = successful ? 0 : 1,
                        LastCall = DateTime.UtcNow 
                    },
                    (k, existing) => 
                    {
                        if (successful)
                            existing.Successful++;
                        else
                            existing.Failed++;
                        existing.LastCall = DateTime.UtcNow;
                        return existing;
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record API call");
            }
        }

        public async Task<string> SanitizeInputAsync(string input, string inputType)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            try
            {
                var sanitized = input;

                switch (inputType.ToLower())
                {
                    case "html":
                        // Remove potentially dangerous HTML tags and attributes
                        sanitized = SanitizeHtml(sanitized);
                        break;
                    
                    case "sql":
                        // Remove SQL injection patterns
                        sanitized = SanitizeSql(sanitized);
                        break;
                    
                    case "filename":
                        // Sanitize filename
                        sanitized = SanitizeFilename(sanitized);
                        break;
                    
                    case "url":
                        // Validate and sanitize URL
                        sanitized = SanitizeUrl(sanitized);
                        break;
                    
                    case "email":
                        // Validate email format
                        sanitized = SanitizeEmail(sanitized);
                        break;
                    
                    default:
                        // General sanitization
                        sanitized = SanitizeGeneral(sanitized);
                        break;
                }

                return sanitized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sanitize input of type {InputType}", inputType);
                return string.Empty; // Return empty string on sanitization failure
            }
        }

        public async Task<bool> IsInputSafeAsync(string input, string inputType)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            try
            {
                var sanitized = await SanitizeInputAsync(input, inputType);
                return input == sanitized;
            }
            catch
            {
                return false;
            }
        }

        #region Private Methods

        private int GetMaxRequestsForEndpoint(string endpoint)
        {
            // Configure different rate limits for different endpoint types
            var endpointLimits = new Dictionary<string, int>
            {
                { "/api/auth/login", 10 },      // 10 login attempts per minute
                { "/api/auth/refresh", 20 },    // 20 refresh attempts per minute
                { "/api/reports", 30 },         // 30 report requests per minute
                { "/api/export", 5 },           // 5 export requests per minute (resource intensive)
                { "/api/analytics", 50 },       // 50 analytics requests per minute
            };

            foreach (var limit in endpointLimits)
            {
                if (endpoint.StartsWith(limit.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return limit.Value;
                }
            }

            return 60; // Default: 60 requests per minute
        }

        private string SanitizeHtml(string input)
        {
            // Remove script tags and dangerous attributes
            var scriptPattern = @"<script[^>]*>.*?</script>";
            var onEventPattern = @"\s*on\w+\s*=\s*[""'][^""']*[""']";
            var javascriptPattern = @"javascript:";

            input = Regex.Replace(input, scriptPattern, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            input = Regex.Replace(input, onEventPattern, "", RegexOptions.IgnoreCase);
            input = Regex.Replace(input, javascriptPattern, "", RegexOptions.IgnoreCase);

            return input;
        }

        private string SanitizeSql(string input)
        {
            // Remove common SQL injection patterns
            var sqlPatterns = new[]
            {
                @"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE){0,1}|INSERT( +INTO){0,1}|MERGE|SELECT|UPDATE|UNION( +ALL){0,1})\b)",
                @"(\b(AND|OR)\b.{1,6}?(=|>|<|\!|\||&))",
                @"(\b(GRANT|REVOKE)\b)",
                @"(\b(GROUP\s+BY|ORDER\s+BY|HAVING)\b)",
                @"(\bCAST\s*\()",
                @"(\bCONVERT\s*\()",
                @"(\bSUBSTRING\s*\()",
                @"(\bCHAR\s*\()",
                @"(\bASCII\s*\()",
                @"(\bCOUNT\s*\()",
                @"(\bMIN\s*\()",
                @"(\bMAX\s*\()",
                @"(\bSUM\s*\()",
                @"(\bAVG\s*\()"
            };

            foreach (var pattern in sqlPatterns)
            {
                input = Regex.Replace(input, pattern, "", RegexOptions.IgnoreCase);
            }

            return input;
        }

        private string SanitizeFilename(string input)
        {
            // Remove dangerous characters from filename
            var invalidChars = Path.GetInvalidFileNameChars();
            var additionalChars = new[] { '<', '>', ':', '"', '|', '?', '*', '\\', '/' };
            
            foreach (var c in invalidChars.Concat(additionalChars))
            {
                input = input.Replace(c, '_');
            }

            // Limit length
            if (input.Length > 255)
            {
                input = input.Substring(0, 255);
            }

            return input;
        }

        private string SanitizeUrl(string input)
        {
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            {
                // Only allow HTTP and HTTPS schemes
                if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    return uri.ToString();
                }
            }

            return string.Empty;
        }

        private string SanitizeEmail(string input)
        {
            var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            if (Regex.IsMatch(input, emailPattern))
            {
                return input.ToLower().Trim();
            }

            return string.Empty;
        }

        private string SanitizeGeneral(string input)
        {
            // Remove control characters and normalize whitespace
            input = Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
            input = Regex.Replace(input, @"\s+", " ");
            return input.Trim();
        }

        #endregion

        #region Data Models

        private class ThrottleInfo
        {
            public int RequestCount { get; set; }
            public DateTime WindowStart { get; set; }
            public DateTime LastRequest { get; set; }
        }

        private class ApiCallRecord
        {
            public string Endpoint { get; set; } = string.Empty;
            public string ClientId { get; set; } = string.Empty;
            public int Successful { get; set; }
            public int Failed { get; set; }
            public DateTime LastCall { get; set; }
        }

        #endregion
    }
}
