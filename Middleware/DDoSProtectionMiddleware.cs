using System.Collections.Concurrent;
using System.Net;

namespace MultiDeptReportingTool.Middleware
{
    public class DDoSProtectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DDoSProtectionMiddleware> _logger;
        private readonly IConfiguration _configuration;
        
        // In-memory storage for request tracking (use Redis in production)
        private static readonly ConcurrentDictionary<string, ClientRequestInfo> ClientRequests = new();
        private static readonly ConcurrentDictionary<string, DateTime> BannedIPs = new();
        
        // Configuration
        private readonly int _maxRequestsPerMinute;
        private readonly int _maxRequestsPerHour;
        private readonly int _maxRequestsPerDay;
        private readonly int _banDurationMinutes;
        private readonly int _suspiciousThreshold;
        private readonly string[] _protectedEndpoints;
        private readonly string[] _whitelistedIPs;

        public DDoSProtectionMiddleware(
            RequestDelegate next,
            ILogger<DDoSProtectionMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
            
            _maxRequestsPerMinute = configuration.GetValue<int>("DDoSProtection:MaxRequestsPerMinute", 60);
            _maxRequestsPerHour = configuration.GetValue<int>("DDoSProtection:MaxRequestsPerHour", 1000);
            _maxRequestsPerDay = configuration.GetValue<int>("DDoSProtection:MaxRequestsPerDay", 10000);
            _banDurationMinutes = configuration.GetValue<int>("DDoSProtection:BanDurationMinutes", 30);
            _suspiciousThreshold = configuration.GetValue<int>("DDoSProtection:SuspiciousThreshold", 100);
            _protectedEndpoints = configuration.GetSection("DDoSProtection:ProtectedEndpoints").Get<string[]>() ?? Array.Empty<string>();
            _whitelistedIPs = configuration.GetSection("DDoSProtection:WhitelistedIPs").Get<string[]>() ?? Array.Empty<string>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIP = GetClientIP(context);
            
            // Skip protection for whitelisted IPs
            if (_whitelistedIPs.Contains(clientIP))
            {
                await _next(context);
                return;
            }

            // Check if IP is currently banned
            if (IsIPBanned(clientIP))
            {
                await HandleBannedRequest(context, clientIP);
                return;
            }

            // Check rate limits
            var rateLimitResult = CheckRateLimit(clientIP, context.Request.Path);
            if (!rateLimitResult.IsAllowed)
            {
                await HandleRateLimitExceeded(context, clientIP, rateLimitResult);
                return;
            }

        // Track the request
        TrackRequest(clientIP, context.Request.Path, context.Request.Headers["User-Agent"].ToString());            // Check for suspicious patterns
            var suspiciousScore = CalculateSuspiciousScore(clientIP, context);
            if (suspiciousScore >= _suspiciousThreshold)
            {
                await HandleSuspiciousActivity(context, clientIP, suspiciousScore);
                return;
            }

            // Add rate limit headers
            AddRateLimitHeaders(context, rateLimitResult);

            await _next(context);
        }

        private string GetClientIP(HttpContext context)
        {
            // Check for forwarded IP headers (for load balancers/proxies)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIP))
            {
                return realIP;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private bool IsIPBanned(string clientIP)
        {
            if (BannedIPs.TryGetValue(clientIP, out var banTime))
            {
                if (DateTime.UtcNow.Subtract(banTime).TotalMinutes < _banDurationMinutes)
                {
                    return true;
                }
                else
                {
                    // Ban expired, remove it
                    BannedIPs.TryRemove(clientIP, out _);
                    return false;
                }
            }
            return false;
        }

        private RateLimitResult CheckRateLimit(string clientIP, string path)
        {
            var now = DateTime.UtcNow;
            var clientInfo = ClientRequests.GetOrAdd(clientIP, _ => new ClientRequestInfo());

            lock (clientInfo)
            {
                // Clean old requests
                clientInfo.CleanOldRequests(now);

                var result = new RateLimitResult
                {
                    IsAllowed = true,
                    RequestsInLastMinute = clientInfo.GetRequestsInTimespan(now, TimeSpan.FromMinutes(1)),
                    RequestsInLastHour = clientInfo.GetRequestsInTimespan(now, TimeSpan.FromHours(1)),
                    RequestsInLastDay = clientInfo.GetRequestsInTimespan(now, TimeSpan.FromDays(1))
                };

                // Check if this is a protected endpoint (stricter limits)
                var isProtectedEndpoint = _protectedEndpoints.Any(endpoint => 
                    path.ToString().StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));

                var minuteLimit = isProtectedEndpoint ? _maxRequestsPerMinute / 2 : _maxRequestsPerMinute;
                var hourLimit = isProtectedEndpoint ? _maxRequestsPerHour / 2 : _maxRequestsPerHour;
                var dayLimit = isProtectedEndpoint ? _maxRequestsPerDay / 2 : _maxRequestsPerDay;

                // Check rate limits
                if (result.RequestsInLastMinute >= minuteLimit)
                {
                    result.IsAllowed = false;
                    result.ResetTimeMinutes = 1;
                    result.Reason = "Rate limit exceeded: too many requests per minute";
                }
                else if (result.RequestsInLastHour >= hourLimit)
                {
                    result.IsAllowed = false;
                    result.ResetTimeMinutes = 60;
                    result.Reason = "Rate limit exceeded: too many requests per hour";
                }
                else if (result.RequestsInLastDay >= dayLimit)
                {
                    result.IsAllowed = false;
                    result.ResetTimeMinutes = 1440;
                    result.Reason = "Rate limit exceeded: too many requests per day";
                }

                result.RemainingRequests = minuteLimit - result.RequestsInLastMinute;

                return result;
            }
        }

        private void TrackRequest(string clientIP, string path, string? userAgent)
        {
            var clientInfo = ClientRequests.GetOrAdd(clientIP, _ => new ClientRequestInfo());
            
            lock (clientInfo)
            {
                clientInfo.AddRequest(DateTime.UtcNow, path, userAgent);
            }
        }

        private int CalculateSuspiciousScore(string clientIP, HttpContext context)
        {
            var clientInfo = ClientRequests.GetOrAdd(clientIP, _ => new ClientRequestInfo());
            var score = 0;
            var now = DateTime.UtcNow;

            lock (clientInfo)
            {
                // High request frequency
                var recentRequests = clientInfo.GetRequestsInTimespan(now, TimeSpan.FromMinutes(1));
                if (recentRequests > _maxRequestsPerMinute * 0.8)
                {
                    score += 30;
                }

                // Suspicious user agents
                var userAgent = context.Request.Headers["User-Agent"].ToString() ?? "";
                if (IsSuspiciousUserAgent(userAgent))
                {
                    score += 40;
                }

                // Unusual request patterns
                var requestPaths = clientInfo.GetRecentPaths(now, TimeSpan.FromMinutes(5));
                if (HasSuspiciousRequestPattern(requestPaths))
                {
                    score += 35;
                }

                // Multiple different user agents from same IP
                var userAgents = clientInfo.GetRecentUserAgents(now, TimeSpan.FromHours(1));
                if (userAgents.Count > 5)
                {
                    score += 25;
                }

                // Sequential requests (potential bot behavior)
                if (clientInfo.HasSequentialRequests(now, TimeSpan.FromSeconds(10)))
                {
                    score += 20;
                }

                return score;
            }
        }

        private bool IsSuspiciousUserAgent(string userAgent)
        {
            var suspiciousPatterns = new[]
            {
                "bot", "crawler", "spider", "scraper", "wget", "curl", "python", "java",
                "scanner", "vulnerability", "nikto", "sqlmap", "nmap", "masscan"
            };

            userAgent = userAgent.ToLowerInvariant();
            return suspiciousPatterns.Any(pattern => userAgent.Contains(pattern)) ||
                   string.IsNullOrEmpty(userAgent) ||
                   userAgent.Length < 10;
        }

        private bool HasSuspiciousRequestPattern(List<string> paths)
        {
            var suspiciousPatterns = new[]
            {
                "/admin", "/wp-admin", "/phpmyadmin", "/.env", "/config", "/backup",
                "/test", "/debug", "/api/v", "/swagger", "/.git", "/sensitive"
            };

            var suspiciousCount = paths.Count(path => 
                suspiciousPatterns.Any(pattern => 
                    path.Contains(pattern, StringComparison.OrdinalIgnoreCase)));

            return suspiciousCount > paths.Count * 0.5; // More than 50% suspicious requests
        }

        private async Task HandleBannedRequest(HttpContext context, string clientIP)
        {
            _logger.LogWarning("Blocked request from banned IP: {ClientIP}", clientIP);
            
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.Add("Retry-After", (_banDurationMinutes * 60).ToString());
            
            await context.Response.WriteAsync("IP temporarily banned due to suspicious activity");
        }

        private async Task HandleRateLimitExceeded(HttpContext context, string clientIP, RateLimitResult result)
        {
            _logger.LogWarning("Rate limit exceeded for IP {ClientIP}: {Reason}", clientIP, result.Reason);
            
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.Add("Retry-After", (result.ResetTimeMinutes * 60).ToString());
            context.Response.Headers.Add("X-RateLimit-Limit", _maxRequestsPerMinute.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", result.RemainingRequests.ToString());
            context.Response.Headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddMinutes(result.ResetTimeMinutes).ToUnixTimeSeconds().ToString());
            
            await context.Response.WriteAsync($"Rate limit exceeded: {result.Reason}");
        }

        private async Task HandleSuspiciousActivity(HttpContext context, string clientIP, int score)
        {
            _logger.LogWarning("Suspicious activity detected from IP {ClientIP} with score {Score}", clientIP, score);
            
            // Ban IP for repeated suspicious activity
            if (score >= _suspiciousThreshold * 1.5)
            {
                BannedIPs.TryAdd(clientIP, DateTime.UtcNow);
                _logger.LogWarning("IP {ClientIP} has been temporarily banned due to high suspicious activity score: {Score}", clientIP, score);
            }
            
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            await context.Response.WriteAsync("Request blocked due to suspicious activity");
        }

        private void AddRateLimitHeaders(HttpContext context, RateLimitResult result)
        {
            context.Response.Headers.Add("X-RateLimit-Limit", _maxRequestsPerMinute.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", result.RemainingRequests.ToString());
            context.Response.Headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString());
        }

        // Cleanup method to remove old client data (should be called periodically)
        public static void CleanupOldData()
        {
            var cutoff = DateTime.UtcNow.AddDays(-1);
            var keysToRemove = new List<string>();

            foreach (var kvp in ClientRequests)
            {
                if (kvp.Value.LastRequestTime < cutoff)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                ClientRequests.TryRemove(key, out _);
            }

            // Clean expired bans
            var expiredBans = BannedIPs.Where(kvp => 
                DateTime.UtcNow.Subtract(kvp.Value).TotalMinutes > 30).Select(kvp => kvp.Key).ToList();

            foreach (var ip in expiredBans)
            {
                BannedIPs.TryRemove(ip, out _);
            }
        }
    }

    public class ClientRequestInfo
    {
        private readonly List<RequestInfo> _requests = new();
        public DateTime LastRequestTime { get; private set; } = DateTime.UtcNow;

        public void AddRequest(DateTime timestamp, string path, string? userAgent)
        {
            _requests.Add(new RequestInfo
            {
                Timestamp = timestamp,
                Path = path,
                UserAgent = userAgent ?? ""
            });
            LastRequestTime = timestamp;
        }

        public int GetRequestsInTimespan(DateTime now, TimeSpan timespan)
        {
            var cutoff = now.Subtract(timespan);
            return _requests.Count(r => r.Timestamp >= cutoff);
        }

        public List<string> GetRecentPaths(DateTime now, TimeSpan timespan)
        {
            var cutoff = now.Subtract(timespan);
            return _requests.Where(r => r.Timestamp >= cutoff).Select(r => r.Path).ToList();
        }

        public HashSet<string> GetRecentUserAgents(DateTime now, TimeSpan timespan)
        {
            var cutoff = now.Subtract(timespan);
            return _requests.Where(r => r.Timestamp >= cutoff)
                           .Select(r => r.UserAgent)
                           .Where(ua => !string.IsNullOrEmpty(ua))
                           .ToHashSet();
        }

        public bool HasSequentialRequests(DateTime now, TimeSpan timespan)
        {
            var cutoff = now.Subtract(timespan);
            var recentRequests = _requests.Where(r => r.Timestamp >= cutoff)
                                         .OrderBy(r => r.Timestamp)
                                         .ToList();

            if (recentRequests.Count < 5) return false;

            // Check if requests are too regular (potential bot)
            var intervals = new List<double>();
            for (int i = 1; i < recentRequests.Count; i++)
            {
                intervals.Add(recentRequests[i].Timestamp.Subtract(recentRequests[i-1].Timestamp).TotalMilliseconds);
            }

            var avgInterval = intervals.Average();
            var variance = intervals.Sum(x => Math.Pow(x - avgInterval, 2)) / intervals.Count;
            
            // Low variance indicates very regular timing (suspicious)
            return variance < 100; // Less than 100ms variance
        }

        public void CleanOldRequests(DateTime now)
        {
            var cutoff = now.AddDays(-1);
            _requests.RemoveAll(r => r.Timestamp < cutoff);
        }
    }

    public class RequestInfo
    {
        public DateTime Timestamp { get; set; }
        public string Path { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
    }

    public class RateLimitResult
    {
        public bool IsAllowed { get; set; }
        public int RequestsInLastMinute { get; set; }
        public int RequestsInLastHour { get; set; }
        public int RequestsInLastDay { get; set; }
        public int RemainingRequests { get; set; }
        public int ResetTimeMinutes { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
