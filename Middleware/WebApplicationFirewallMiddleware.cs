using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace MultiDeptReportingTool.Middleware
{
    public class WebApplicationFirewallMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebApplicationFirewallMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly bool _isEnabled;

        // WAF Rules
        private static readonly Dictionary<string, Regex[]> SecurityRules = new()
        {
            ["sql_injection"] = new[]
            {
                new Regex(@"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE)\b)", RegexOptions.IgnoreCase),
                new Regex(@"(\bunion\b.*\bselect\b)", RegexOptions.IgnoreCase),
                new Regex(@"(\b(or|and)\b\s*\d+\s*=\s*\d+)", RegexOptions.IgnoreCase),
                new Regex(@"(\b(or|and)\b\s*['""].*['""])", RegexOptions.IgnoreCase),
                new Regex(@"(--|\#|/\*|\*/)", RegexOptions.IgnoreCase),
                new Regex(@"(\bxp_\w+\b)", RegexOptions.IgnoreCase),
                new Regex(@"(\bsp_\w+\b)", RegexOptions.IgnoreCase),
                new Regex(@"(\b(sysobjects|syscolumns|systables)\b)", RegexOptions.IgnoreCase)
            },
            ["xss"] = new[]
            {
                new Regex(@"(<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>)", RegexOptions.IgnoreCase),
                new Regex(@"(javascript\s*:)", RegexOptions.IgnoreCase),
                new Regex(@"(vbscript\s*:)", RegexOptions.IgnoreCase),
                new Regex(@"(on\w+\s*=)", RegexOptions.IgnoreCase),
                new Regex(@"(<iframe\b[^>]*>)", RegexOptions.IgnoreCase),
                new Regex(@"(<object\b[^>]*>)", RegexOptions.IgnoreCase),
                new Regex(@"(<embed\b[^>]*>)", RegexOptions.IgnoreCase),
                new Regex(@"(<link\b[^>]*>)", RegexOptions.IgnoreCase),
                new Regex(@"(<img\b[^>]*onerror)", RegexOptions.IgnoreCase),
                new Regex(@"(eval\s*\()", RegexOptions.IgnoreCase),
                new Regex(@"(expression\s*\()", RegexOptions.IgnoreCase)
            },
            ["path_traversal"] = new[]
            {
                new Regex(@"(\.\.[/\\])", RegexOptions.IgnoreCase),
                new Regex(@"(\/etc\/passwd)", RegexOptions.IgnoreCase),
                new Regex(@"(\/etc\/shadow)", RegexOptions.IgnoreCase),
                new Regex(@"(\/windows\/system32)", RegexOptions.IgnoreCase),
                new Regex(@"(\.\.%2f)", RegexOptions.IgnoreCase),
                new Regex(@"(\.\.%5c)", RegexOptions.IgnoreCase),
                new Regex(@"(%2e%2e%2f)", RegexOptions.IgnoreCase),
                new Regex(@"(%2e%2e%5c)", RegexOptions.IgnoreCase)
            },
            ["command_injection"] = new[]
            {
                new Regex(@"(\b(cmd|bash|sh|powershell|pwsh)\b)", RegexOptions.IgnoreCase),
                new Regex(@"(\b(wget|curl|nc|netcat)\b)", RegexOptions.IgnoreCase),
                new Regex(@"(\||\&\&|\|\|)", RegexOptions.IgnoreCase),
                new Regex(@"(\$\(.*\))", RegexOptions.IgnoreCase),
                new Regex(@"(`.*`)", RegexOptions.IgnoreCase),
                new Regex(@"(\b(cat|ls|dir|type|copy|move|del|rm)\b)", RegexOptions.IgnoreCase)
            },
            ["ldap_injection"] = new[]
            {
                new Regex(@"(\(\|)", RegexOptions.IgnoreCase),
                new Regex(@"(\)\()", RegexOptions.IgnoreCase),
                new Regex(@"(\(\&)", RegexOptions.IgnoreCase),
                new Regex(@"(\*\))", RegexOptions.IgnoreCase),
                new Regex(@"(\(\!\(\|)", RegexOptions.IgnoreCase)
            },
            ["php_injection"] = new[]
            {
                new Regex(@"(<\?php)", RegexOptions.IgnoreCase),
                new Regex(@"(<\?\s)", RegexOptions.IgnoreCase),
                new Regex(@"(\bphp\b)", RegexOptions.IgnoreCase),
                new Regex(@"(\binclude\b|\brequire\b)", RegexOptions.IgnoreCase),
                new Regex(@"(\bfile_get_contents\b)", RegexOptions.IgnoreCase),
                new Regex(@"(\bshell_exec\b)", RegexOptions.IgnoreCase),
                new Regex(@"(\bsystem\b)", RegexOptions.IgnoreCase),
                new Regex(@"(\bpassthru\b)", RegexOptions.IgnoreCase)
            }
        };

        // Suspicious patterns
        private static readonly Regex[] SuspiciousPatterns = new[]
        {
            new Regex(@"(\b(hack|exploit|vulnerability|injection)\b)", RegexOptions.IgnoreCase),
            new Regex(@"(base64_decode|urldecode)", RegexOptions.IgnoreCase),
            new Regex(@"(document\.cookie)", RegexOptions.IgnoreCase),
            new Regex(@"(window\.location)", RegexOptions.IgnoreCase),
            new Regex(@"(\bchar\b.*\b\d+\b)", RegexOptions.IgnoreCase)
        };

        // File inclusion patterns
        private static readonly Regex[] FileInclusionPatterns = new[]
        {
            new Regex(@"(http[s]?:\/\/)", RegexOptions.IgnoreCase),
            new Regex(@"(ftp:\/\/)", RegexOptions.IgnoreCase),
            new Regex(@"(file:\/\/)", RegexOptions.IgnoreCase),
            new Regex(@"(\binclude\b.*http)", RegexOptions.IgnoreCase),
            new Regex(@"(\brequire\b.*http)", RegexOptions.IgnoreCase)
        };

        public WebApplicationFirewallMiddleware(
            RequestDelegate next,
            ILogger<WebApplicationFirewallMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
            _isEnabled = configuration.GetValue<bool>("WAF:Enabled", true);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_isEnabled)
            {
                await _next(context);
                return;
            }

            try
            {
                var detectionResult = await AnalyzeRequestAsync(context);
                
                if (detectionResult.IsBlocked)
                {
                    await HandleBlockedRequestAsync(context, detectionResult);
                    return;
                }

                if (detectionResult.Warnings.Count > 0)
                {
                    LogWarnings(context, detectionResult);
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WAF middleware");
                await _next(context); // Continue on error to avoid breaking the application
            }
        }

        private async Task<WafDetectionResult> AnalyzeRequestAsync(HttpContext context)
        {
            var result = new WafDetectionResult();
            var request = context.Request;

            // Analyze URL
            AnalyzeUrl(request.Path + request.QueryString, result);

            // Analyze headers
            AnalyzeHeaders(request.Headers, result);

            // Analyze query parameters
            foreach (var param in request.Query)
            {
                AnalyzeParameter(param.Key, param.Value, result, "Query");
            }

            // Analyze form data
            if (request.HasFormContentType)
            {
                try
                {
                    var form = await request.ReadFormAsync();
                    foreach (var field in form)
                    {
                        AnalyzeParameter(field.Key, field.Value, result, "Form");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading form data for WAF analysis");
                }
            }

            // Analyze JSON body
            if (request.ContentType?.Contains("application/json") == true)
            {
                await AnalyzeJsonBodyAsync(request, result);
            }

            // Calculate risk score
            result.RiskScore = CalculateRiskScore(result);
            
            // Determine if request should be blocked
            var blockThreshold = _configuration.GetValue<int>("WAF:BlockThreshold", 80);
            result.IsBlocked = result.RiskScore >= blockThreshold;

            return result;
        }

        private void AnalyzeUrl(string url, WafDetectionResult result)
        {
            var decodedUrl = HttpUtility.UrlDecode(url);
            
            // Check for multiple encoding attempts
            var doubleDecoded = HttpUtility.UrlDecode(decodedUrl);
            if (decodedUrl != doubleDecoded)
            {
                result.Violations.Add($"Multiple URL encoding detected: {url}");
                result.RiskScore += 30;
            }

            CheckSecurityRules(decodedUrl, result, "URL");
            CheckSuspiciousPatterns(decodedUrl, result, "URL");
            CheckFileInclusion(decodedUrl, result, "URL");
        }

        private void AnalyzeHeaders(IHeaderDictionary headers, WafDetectionResult result)
        {
            var suspiciousHeaders = new[]
            {
                "X-Forwarded-For", "X-Real-IP", "X-Originating-IP", "X-Remote-IP",
                "User-Agent", "Referer", "Cookie"
            };

            foreach (var header in headers)
            {
                if (suspiciousHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
                {
                    foreach (var value in header.Value)
                    {
                        CheckSecurityRules(value, result, $"Header[{header.Key}]");
                        CheckSuspiciousPatterns(value, result, $"Header[{header.Key}]");
                    }
                }

                // Check for unusual headers
                if (header.Key.StartsWith("X-") && !IsKnownHeader(header.Key))
                {
                    result.Warnings.Add($"Unusual header detected: {header.Key}");
                }
            }

            // Check User-Agent
            var userAgent = headers["User-Agent"].ToString();
            if (IsSuspiciousUserAgent(userAgent))
            {
                result.Violations.Add($"Suspicious User-Agent: {userAgent}");
                result.RiskScore += 25;
            }
        }

        private void AnalyzeParameter(string name, Microsoft.Extensions.Primitives.StringValues values, WafDetectionResult result, string source)
        {
            foreach (var value in values)
            {
                if (string.IsNullOrEmpty(value)) continue;

                var decodedValue = HttpUtility.UrlDecode(value);
                
                CheckSecurityRules(decodedValue, result, $"{source}[{name}]");
                CheckSuspiciousPatterns(decodedValue, result, $"{source}[{name}]");
                CheckFileInclusion(decodedValue, result, $"{source}[{name}]");

                // Check for long parameter values (potential buffer overflow)
                if (decodedValue.Length > 1000)
                {
                    result.Warnings.Add($"Unusually long parameter value in {source}[{name}]: {decodedValue.Length} characters");
                    result.RiskScore += 10;
                }

                // Check for binary data in parameters
                if (ContainsBinaryData(decodedValue))
                {
                    result.Violations.Add($"Binary data detected in {source}[{name}]");
                    result.RiskScore += 20;
                }
            }
        }

        private async Task AnalyzeJsonBodyAsync(HttpRequest request, WafDetectionResult result)
        {
            try
            {
                request.EnableBuffering();
                request.Body.Position = 0;
                
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;

                if (!string.IsNullOrEmpty(body))
                {
                    CheckSecurityRules(body, result, "JSON Body");
                    CheckSuspiciousPatterns(body, result, "JSON Body");
                    CheckFileInclusion(body, result, "JSON Body");

                    // Try to parse JSON and analyze individual fields
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(body);
                        AnalyzeJsonElement(jsonDoc.RootElement, result, "JSON");
                    }
                    catch (JsonException)
                    {
                        result.Warnings.Add("Invalid JSON format detected");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error analyzing JSON body");
            }
        }

        private void AnalyzeJsonElement(JsonElement element, WafDetectionResult result, string path)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        AnalyzeJsonElement(property.Value, result, $"{path}.{property.Name}");
                    }
                    break;
                case JsonValueKind.Array:
                    var index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        AnalyzeJsonElement(item, result, $"{path}[{index}]");
                        index++;
                    }
                    break;
                case JsonValueKind.String:
                    var value = element.GetString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        CheckSecurityRules(value, result, path);
                        CheckSuspiciousPatterns(value, result, path);
                        CheckFileInclusion(value, result, path);
                    }
                    break;
            }
        }

        private void CheckSecurityRules(string input, WafDetectionResult result, string source)
        {
            foreach (var ruleCategory in SecurityRules)
            {
                foreach (var rule in ruleCategory.Value)
                {
                    if (rule.IsMatch(input))
                    {
                        var violation = $"{ruleCategory.Key.ToUpper()} detected in {source}: {rule.Match(input).Value}";
                        result.Violations.Add(violation);
                        
                        // Assign risk scores based on attack type
                        var riskScore = ruleCategory.Key switch
                        {
                            "sql_injection" => 40,
                            "xss" => 35,
                            "command_injection" => 45,
                            "path_traversal" => 30,
                            "ldap_injection" => 25,
                            "php_injection" => 35,
                            _ => 20
                        };
                        
                        result.RiskScore += riskScore;
                        break; // Only count first match per category
                    }
                }
            }
        }

        private void CheckSuspiciousPatterns(string input, WafDetectionResult result, string source)
        {
            foreach (var pattern in SuspiciousPatterns)
            {
                if (pattern.IsMatch(input))
                {
                    result.Warnings.Add($"Suspicious pattern in {source}: {pattern.Match(input).Value}");
                    result.RiskScore += 15;
                }
            }
        }

        private void CheckFileInclusion(string input, WafDetectionResult result, string source)
        {
            foreach (var pattern in FileInclusionPatterns)
            {
                if (pattern.IsMatch(input))
                {
                    result.Violations.Add($"File inclusion attempt in {source}: {pattern.Match(input).Value}");
                    result.RiskScore += 30;
                }
            }
        }

        private bool IsSuspiciousUserAgent(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent) || userAgent.Length < 10)
                return true;

            var suspiciousAgents = new[]
            {
                "sqlmap", "nikto", "nmap", "masscan", "dirb", "dirbuster", "gobuster",
                "burpsuite", "owasp", "scanner", "vulnerability", "exploit", "hack",
                "bot", "crawler", "spider", "wget", "curl", "python-requests"
            };

            return suspiciousAgents.Any(agent => 
                userAgent.Contains(agent, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsKnownHeader(string headerName)
        {
            var knownHeaders = new[]
            {
                "X-Forwarded-For", "X-Real-IP", "X-Forwarded-Proto", "X-Forwarded-Host",
                "X-Requested-With", "X-CSRF-Token", "X-API-Key", "X-Auth-Token",
                "X-Content-Type-Options", "X-Frame-Options", "X-XSS-Protection"
            };

            return knownHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
        }

        private bool ContainsBinaryData(string input)
        {
            return input.Any(c => c < 32 && c != '\t' && c != '\n' && c != '\r');
        }

        private int CalculateRiskScore(WafDetectionResult result)
        {
            var score = result.RiskScore;
            
            // Increase score based on number of violations
            score += result.Violations.Count * 10;
            
            // Increase score based on number of warnings
            score += result.Warnings.Count * 5;
            
            return Math.Min(score, 100); // Cap at 100
        }

        private async Task HandleBlockedRequestAsync(HttpContext context, WafDetectionResult result)
        {
            var clientIP = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            
            _logger.LogWarning("WAF blocked request from {ClientIP} with risk score {RiskScore}. Violations: {Violations}",
                clientIP, result.RiskScore, string.Join("; ", result.Violations));

            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Request blocked by Web Application Firewall",
                riskScore = result.RiskScore,
                violations = result.Violations.Take(3).ToArray(), // Limit exposed violations
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private void LogWarnings(HttpContext context, WafDetectionResult result)
        {
            var clientIP = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            
            _logger.LogInformation("WAF warnings for request from {ClientIP} with risk score {RiskScore}. Warnings: {Warnings}",
                clientIP, result.RiskScore, string.Join("; ", result.Warnings));
        }
    }

    public class WafDetectionResult
    {
        public List<string> Violations { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int RiskScore { get; set; }
        public bool IsBlocked { get; set; }
    }
}
