using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.Models;
using System.Text.Json;

namespace MultiDeptReportingTool.Services
{
    public class DatabaseSeedingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseSeedingService> _logger;

        public DatabaseSeedingService(ApplicationDbContext context, ILogger<DatabaseSeedingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAnalyticsDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting analytics data seeding...");

                // Clear existing reports and data to start fresh
                await ClearExistingDataAsync();

                // Seed realistic report data for analytics
                await SeedReportsAsync();
                await SeedReportDataAsync();
                await SeedAuditLogsAsync();

                await _context.SaveChangesAsync();
                _logger.LogInformation("Analytics data seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding analytics data");
                throw;
            }
        }

        private async Task ClearExistingDataAsync()
        {
            _logger.LogInformation("Clearing existing analytics data...");
            
            // Clear in order to respect foreign key constraints
            _context.AuditLogs.RemoveRange(_context.AuditLogs);
            _context.ReportData.RemoveRange(_context.ReportData);
            _context.Reports.RemoveRange(_context.Reports);
            
            await _context.SaveChangesAsync();
        }

        private async Task SeedReportsAsync()
        {
            _logger.LogInformation("Seeding reports...");

            var random = new Random(42); // Fixed seed for consistent data
            var now = DateTime.UtcNow;

            // Get users and departments for foreign keys
            var users = await _context.Users.ToListAsync();
            var departments = await _context.Departments.ToListAsync();

            if (!users.Any() || !departments.Any())
            {
                _logger.LogWarning("No users or departments found. Cannot seed reports.");
                return;
            }

            var reports = new List<Report>();

            // Generate reports for the last 12 months
            for (int monthsBack = 12; monthsBack >= 0; monthsBack--)
            {
                var periodStart = now.AddMonths(-monthsBack).AddDays(-now.Day + 1); // First day of month
                var periodEnd = periodStart.AddMonths(1).AddDays(-1); // Last day of month

                foreach (var department in departments)
                {
                    // Generate 1-3 reports per department per month
                    var reportsCount = random.Next(1, 4);
                    
                    for (int i = 0; i < reportsCount; i++)
                    {
                        var createdByUser = users[random.Next(users.Count)];
                        var reportType = GetRandomReportType(random);
                        var status = GetReportStatus(monthsBack, random);
                        
                        var report = new Report
                        {
                            Title = $"{department.Name} {reportType} Report - {periodStart:MMM yyyy}",
                            Description = $"Comprehensive {reportType.ToLower()} report for {department.Name} department covering {periodStart:MMM dd} to {periodEnd:MMM dd, yyyy}",
                            ReportType = reportType,
                            Status = status,
                            DepartmentId = department.Id,
                            CreatedByUserId = createdByUser.Id,
                            ReportPeriodStart = periodStart,
                            ReportPeriodEnd = periodEnd,
                            CreatedAt = periodStart.AddDays(random.Next(0, 5)),
                            SubmittedAt = status != "Draft" ? periodStart.AddDays(random.Next(5, 15)) : null,
                            ApprovedAt = status == "Approved" ? periodStart.AddDays(random.Next(15, 25)) : null,
                            ApprovedByUserId = status == "Approved" ? users.FirstOrDefault(u => u.Role == "Executive")?.Id : null,
                            Comments = status == "Approved" ? "Report approved after review" : 
                                      status == "Pending" ? "Awaiting management review" : null
                        };

                        reports.Add(report);
                    }
                }
            }

            await _context.Reports.AddRangeAsync(reports);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Seeded {reports.Count} reports");
        }

        private async Task SeedReportDataAsync()
        {
            _logger.LogInformation("Seeding report data...");

            var reports = await _context.Reports.ToListAsync();
            var random = new Random(42);
            var reportDataList = new List<ReportData>();

            foreach (var report in reports)
            {
                // Add various metrics based on department
                var metrics = GetDepartmentMetrics(report.DepartmentId);
                
                foreach (var metric in metrics)
                {
                    var value = GenerateMetricValue(metric, report, random);
                    
                    var reportData = new ReportData
                    {
                        ReportId = report.Id,
                        FieldName = metric,
                        FieldType = GetFieldType(metric),
                        FieldValue = value.ToString(),
                        NumericValue = decimal.TryParse(value.ToString(), out var numValue) ? numValue : null,
                        DateValue = DateTime.TryParse(value.ToString(), out var dateValue) ? dateValue : null,
                        CreatedAt = report.CreatedAt.AddHours(random.Next(1, 24))
                    };

                    reportDataList.Add(reportData);
                }
            }

            await _context.ReportData.AddRangeAsync(reportDataList);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Seeded {reportDataList.Count} report data entries");
        }

        private async Task SeedAuditLogsAsync()
        {
            _logger.LogInformation("Seeding audit logs...");

            var users = await _context.Users.ToListAsync();
            var reports = await _context.Reports.ToListAsync();
            var random = new Random(42);
            var auditLogs = new List<AuditLog>();

            // Generate audit logs for user activities
            foreach (var user in users)
            {
                // Login activities
                for (int i = 0; i < random.Next(10, 50); i++)
                {
                    var loginTime = DateTime.UtcNow.AddDays(-random.Next(0, 90)).AddHours(-random.Next(0, 24));
                    
                    auditLogs.Add(new AuditLog
                    {
                        Action = "Login",
                        EntityName = "User",
                        EntityId = user.Id,
                        UserId = user.Id,
                        Timestamp = loginTime,
                        IpAddress = GenerateRandomIpAddress(random),
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
                    });
                }
            }

            // Generate audit logs for report activities
            foreach (var report in reports)
            {
                // Report creation
                auditLogs.Add(new AuditLog
                {
                    Action = "Create",
                    EntityName = "Report",
                    EntityId = report.Id,
                    UserId = report.CreatedByUserId,
                    NewValues = JsonSerializer.Serialize(new { report.Title, report.Status }),
                    Timestamp = report.CreatedAt,
                    IpAddress = GenerateRandomIpAddress(random),
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
                });

                // Report updates
                if (report.Status != "Draft")
                {
                    auditLogs.Add(new AuditLog
                    {
                        Action = "Update",
                        EntityName = "Report",
                        EntityId = report.Id,
                        UserId = report.CreatedByUserId,
                        OldValues = JsonSerializer.Serialize(new { Status = "Draft" }),
                        NewValues = JsonSerializer.Serialize(new { Status = report.Status }),
                        Timestamp = report.SubmittedAt ?? report.CreatedAt.AddDays(1),
                        IpAddress = GenerateRandomIpAddress(random),
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
                    });
                }
            }

            await _context.AuditLogs.AddRangeAsync(auditLogs);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Seeded {auditLogs.Count} audit log entries");
        }

        private string GetRandomReportType(Random random)
        {
            var types = new[] { "Monthly", "Weekly", "Quarterly", "Performance", "Financial", "Compliance", "Operational" };
            return types[random.Next(types.Length)];
        }

        private string GetReportStatus(int monthsBack, Random random)
        {
            // Recent reports more likely to be pending/draft
            if (monthsBack <= 1)
            {
                var statuses = new[] { "Draft", "Pending", "Approved" };
                var weights = new[] { 30, 40, 30 }; // 30% draft, 40% pending, 30% approved
                return GetWeightedRandom(statuses, weights, random);
            }
            else
            {
                var statuses = new[] { "Draft", "Pending", "Approved" };
                var weights = new[] { 10, 20, 70 }; // Older reports mostly approved
                return GetWeightedRandom(statuses, weights, random);
            }
        }

        private string GetWeightedRandom(string[] options, int[] weights, Random random)
        {
            var totalWeight = weights.Sum();
            var randomValue = random.Next(totalWeight);
            var cumulative = 0;

            for (int i = 0; i < options.Length; i++)
            {
                cumulative += weights[i];
                if (randomValue < cumulative)
                    return options[i];
            }

            return options[0];
        }

        private List<string> GetDepartmentMetrics(int departmentId)
        {
            // Base metrics for all departments
            var baseMetrics = new List<string>
            {
                "CompletionRate",
                "ResponseTime",
                "QualityScore",
                "EfficiencyRating",
                "UserSatisfaction",
                "ProcessingTime",
                "ErrorRate",
                "ActiveUsers"
            };

            // Department-specific metrics
            var departmentName = _context.Departments.Find(departmentId)?.Name ?? "Unknown";
            
            return departmentName.ToLower() switch
            {
                "finance" => baseMetrics.Concat(new[] { "RevenueTrend", "CostReduction", "BudgetVariance", "ROI" }).ToList(),
                "hr" => baseMetrics.Concat(new[] { "EmployeeRetention", "RecruitmentTime", "TrainingHours", "PerformanceScore" }).ToList(),
                "operations" => baseMetrics.Concat(new[] { "ProductivityIndex", "DowntimeHours", "ThroughputRate", "MaintenanceCost" }).ToList(),
                "compliance" => baseMetrics.Concat(new[] { "ComplianceScore", "AuditFindings", "RiskLevel", "DocumentationRate" }).ToList(),
                "it" => baseMetrics.Concat(new[] { "SystemUptime", "ResponseTime", "SecurityIncidents", "BackupSuccess" }).ToList(),
                _ => baseMetrics
            };
        }

        private object GenerateMetricValue(string metric, Report report, Random random)
        {
            // Generate realistic values based on metric type
            return metric.ToLower() switch
            {
                "completionrate" => Math.Round(60 + random.NextDouble() * 35, 1), // 60-95%
                "responsetime" => Math.Round(1 + random.NextDouble() * 8, 1), // 1-9 days
                "qualityscore" => Math.Round(70 + random.NextDouble() * 25, 1), // 70-95%
                "efficiencyrating" => Math.Round(65 + random.NextDouble() * 30, 1), // 65-95%
                "usersatisfaction" => Math.Round(3.5 + random.NextDouble() * 1.5, 1), // 3.5-5.0
                "processingtime" => Math.Round(0.5 + random.NextDouble() * 4.5, 2), // 0.5-5 hours
                "errorrate" => Math.Round(random.NextDouble() * 5, 2), // 0-5%
                "activeusers" => random.Next(1, 15), // 1-15 users
                "revenuetrend" => Math.Round(-10 + random.NextDouble() * 30, 1), // -10% to +20%
                "costreduction" => Math.Round(random.NextDouble() * 15, 1), // 0-15%
                "budgetvariance" => Math.Round(-5 + random.NextDouble() * 15, 1), // -5% to +10%
                "roi" => Math.Round(5 + random.NextDouble() * 20, 1), // 5-25%
                "employeeretention" => Math.Round(80 + random.NextDouble() * 15, 1), // 80-95%
                "recruitmenttime" => random.Next(15, 60), // 15-60 days
                "traininghours" => random.Next(8, 40), // 8-40 hours
                "performancescore" => Math.Round(3.0 + random.NextDouble() * 2, 1), // 3.0-5.0
                "productivityindex" => Math.Round(85 + random.NextDouble() * 15, 1), // 85-100%
                "downtimehours" => Math.Round(random.NextDouble() * 8, 1), // 0-8 hours
                "throughputrate" => Math.Round(80 + random.NextDouble() * 20, 1), // 80-100%
                "maintenancecost" => random.Next(1000, 10000), // $1K-$10K
                "compliancescore" => Math.Round(85 + random.NextDouble() * 15, 1), // 85-100%
                "auditfindings" => random.Next(0, 5), // 0-5 findings
                "risklevel" => new[] { "Low", "Medium", "High" }[random.Next(3)],
                "documentationrate" => Math.Round(75 + random.NextDouble() * 25, 1), // 75-100%
                "systemuptime" => Math.Round(98 + random.NextDouble() * 2, 2), // 98-100%
                "securityincidents" => random.Next(0, 3), // 0-3 incidents
                "backupsuccess" => Math.Round(95 + random.NextDouble() * 5, 1), // 95-100%
                _ => Math.Round(50 + random.NextDouble() * 50, 1) // Default: 50-100
            };
        }

        private string GetFieldType(string metric)
        {
            return metric.ToLower() switch
            {
                var m when m.Contains("rate") || m.Contains("score") || m.Contains("time") || m.Contains("variance") => "Number",
                var m when m.Contains("users") || m.Contains("findings") || m.Contains("incidents") || m.Contains("hours") => "Number",
                var m when m.Contains("level") => "Text",
                _ => "Number"
            };
        }

        private string GenerateRandomIpAddress(Random random)
        {
            return $"192.168.{random.Next(1, 255)}.{random.Next(1, 255)}";
        }
    }
}
