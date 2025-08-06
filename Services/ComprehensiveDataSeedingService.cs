using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.Models;
using System.Text.Json;

namespace MultiDeptReportingTool.Services
{
    public class ComprehensiveDataSeedingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ComprehensiveDataSeedingService> _logger;
        private readonly Random _random;

        public ComprehensiveDataSeedingService(ApplicationDbContext context, ILogger<ComprehensiveDataSeedingService> logger)
        {
            _context = context;
            _logger = logger;
            _random = new Random(42); // Fixed seed for consistent data
        }

        public async Task SeedComprehensiveDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting comprehensive data seeding...");

                // Clear existing data
                await ClearExistingDataAsync();

                // Get departments and users
                var departments = await _context.Departments.ToListAsync();
                var users = await _context.Users.ToListAsync();

                if (!departments.Any() || !users.Any())
                {
                    _logger.LogWarning("No departments or users found. Cannot seed data.");
                    return;
                }

                // Seed comprehensive data
                await SeedReportsWithRichDataAsync(departments, users);
                await SeedReportDataWithMetricsAsync();
                await SeedAuditLogsWithActivityAsync(users);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Comprehensive data seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding comprehensive data");
                throw;
            }
        }

        private async Task ClearExistingDataAsync()
        {
            _logger.LogInformation("Clearing existing data...");
            
            _context.AuditLogs.RemoveRange(_context.AuditLogs);
            _context.ReportData.RemoveRange(_context.ReportData);
            _context.Reports.RemoveRange(_context.Reports);
            
            await _context.SaveChangesAsync();
        }

        private async Task SeedReportsWithRichDataAsync(List<Department> departments, List<Users> users)
        {
            _logger.LogInformation("Seeding reports with rich data...");

            var now = DateTime.UtcNow;
            var reports = new List<Report>();

            foreach (var department in departments)
            {
                var departmentUsers = users.Where(u => u.DepartmentId == department.Id).ToList();
                if (!departmentUsers.Any())
                {
                    departmentUsers = users.Where(u => u.Role == "Executive").ToList();
                }

                // Generate reports for the last 12 months with realistic patterns
                for (int monthsBack = 12; monthsBack >= 0; monthsBack--)
                {
                    var monthStart = now.AddMonths(-monthsBack).AddDays(-now.Day + 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var reportConfigs = GetDepartmentReportConfigs(department.Name);
                    
                    foreach (var config in reportConfigs)
                    {
                        // Generate multiple reports per type over the year
                        var reportsCount = GetReportsCountForMonth(monthsBack, config.Frequency);
                        
                        for (int i = 0; i < reportsCount; i++)
                        {
                            var createdByUser = departmentUsers[_random.Next(departmentUsers.Count)];
                            var status = GetRealisticsStatus(monthsBack, department.Name, config);
                            var createdDate = GetRandomDateInMonth(monthStart, monthEnd);

                            var report = new Report
                            {
                                Title = GenerateReportTitle(department.Name, config.Type, createdDate, i),
                                Description = GenerateReportDescription(department.Name, config.Type, createdDate),
                                ReportType = config.Type,
                                Status = status,
                                DepartmentId = department.Id,
                                CreatedByUserId = createdByUser.Id,
                                ReportPeriodStart = GetReportPeriodStart(createdDate, config.Type),
                                ReportPeriodEnd = GetReportPeriodEnd(createdDate, config.Type),
                                CreatedAt = createdDate,
                                SubmittedAt = GetSubmittedDate(createdDate, status),
                                ApprovedAt = GetApprovedDate(createdDate, status),
                                ApprovedByUserId = GetApproverUser(status, users),
                                Comments = GenerateStatusComments(status, department.Name, config.Type)
                            };

                            reports.Add(report);
                        }
                    }
                }
            }

            await _context.Reports.AddRangeAsync(reports);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {ReportsCount} reports with rich data", reports.Count);
        }

        private async Task SeedReportDataWithMetricsAsync()
        {
            _logger.LogInformation("Seeding report data with comprehensive metrics...");

            var reports = await _context.Reports.Include(r => r.Department).ToListAsync();
            var reportDataList = new List<ReportData>();

            foreach (var report in reports)
            {
                var metrics = GetComprehensiveMetrics(report.Department!.Name, report.ReportType);
                
                foreach (var metric in metrics)
                {
                    var value = GenerateRealisticMetricValue(
                        metric, 
                        report.Department.Name, 
                        report.ReportType, 
                        report.CreatedAt, 
                        report.Status);
                    
                    var reportData = new ReportData
                    {
                        ReportId = report.Id,
                        FieldName = metric.Name,
                        FieldType = metric.Type,
                        FieldValue = FormatValue(value, metric.Type),
                        NumericValue = ExtractNumericValue(value),
                        DateValue = ExtractDateValue(value),
                        CreatedAt = report.CreatedAt.AddHours(_random.Next(1, 48)),
                        UpdatedAt = report.Status == "Draft" ? null : report.SubmittedAt?.AddHours(_random.Next(1, 24))
                    };

                    reportDataList.Add(reportData);
                }
            }

            await _context.ReportData.AddRangeAsync(reportDataList);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {DataCount} report data entries with metrics", reportDataList.Count);
        }

        private async Task SeedAuditLogsWithActivityAsync(List<Users> users)
        {
            _logger.LogInformation("Seeding audit logs with realistic activity...");

            var reports = await _context.Reports.ToListAsync();
            var auditLogs = new List<AuditLog>();

            // Generate user login activities
            foreach (var user in users)
            {
                var loginFrequency = GetUserLoginFrequency(user.Role, user.DepartmentId);
                
                for (int i = 0; i < loginFrequency; i++)
                {
                    var loginTime = DateTime.UtcNow.AddDays(-_random.Next(0, 180)).AddHours(-_random.Next(6, 20));
                    
                    auditLogs.Add(new AuditLog
                    {
                        Action = "Login",
                        EntityName = "User",
                        EntityId = user.Id,
                        UserId = user.Id,
                        Timestamp = loginTime,
                        IpAddress = GenerateRealisticIpAddress(),
                        UserAgent = GetRealisticUserAgent()
                    });
                }
            }

            // Generate report-related activities
            foreach (var report in reports)
            {
                // Report creation
                auditLogs.Add(CreateAuditLog("Create", "Report", report.Id, report.CreatedByUserId, 
                    null, JsonSerializer.Serialize(new { report.Title, report.Status, report.ReportType }), 
                    report.CreatedAt));

                // Report updates based on status progression
                if (report.Status != "Draft")
                {
                    auditLogs.Add(CreateAuditLog("Update", "Report", report.Id, report.CreatedByUserId,
                        JsonSerializer.Serialize(new { Status = "Draft" }),
                        JsonSerializer.Serialize(new { Status = report.Status }),
                        report.SubmittedAt ?? report.CreatedAt.AddDays(1)));
                }

                if (report.Status == "Approved" && report.ApprovedByUserId.HasValue)
                {
                    auditLogs.Add(CreateAuditLog("Approve", "Report", report.Id, report.ApprovedByUserId.Value,
                        JsonSerializer.Serialize(new { Status = "Pending" }),
                        JsonSerializer.Serialize(new { Status = "Approved", ApprovedAt = report.ApprovedAt }),
                        report.ApprovedAt ?? report.CreatedAt.AddDays(7)));
                }
            }

            await _context.AuditLogs.AddRangeAsync(auditLogs);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {AuditCount} audit log entries", auditLogs.Count);
        }

        #region Helper Methods

        private List<ReportConfig> GetDepartmentReportConfigs(string departmentName)
        {
            return departmentName.ToLower() switch
            {
                "finance" => new List<ReportConfig>
                {
                    new("Monthly Financial", "Monthly"),
                    new("Quarterly Budget", "Quarterly"),
                    new("Annual Revenue", "Yearly"),
                    new("Cost Analysis", "Monthly"),
                    new("Investment Portfolio", "Quarterly"),
                    new("Tax Compliance", "Quarterly"),
                    new("Financial Audit", "Yearly")
                },
                "human resources" => new List<ReportConfig>
                {
                    new("Monthly Performance", "Monthly"),
                    new("Quarterly Recruitment", "Quarterly"),
                    new("Annual Training", "Yearly"),
                    new("Employee Satisfaction", "Quarterly"),
                    new("Compensation Review", "Yearly"),
                    new("Benefits Analysis", "Quarterly"),
                    new("HR Compliance", "Monthly")
                },
                "operations" => new List<ReportConfig>
                {
                    new("Daily Operations", "Daily"),
                    new("Weekly Production", "Weekly"),
                    new("Monthly Efficiency", "Monthly"),
                    new("Quality Control", "Weekly"),
                    new("Safety Report", "Monthly"),
                    new("Maintenance Schedule", "Monthly"),
                    new("Process Optimization", "Quarterly")
                },
                "compliance" => new List<ReportConfig>
                {
                    new("Monthly Compliance", "Monthly"),
                    new("Quarterly Risk Assessment", "Quarterly"),
                    new("Annual Audit", "Yearly"),
                    new("Regulatory Update", "Quarterly"),
                    new("Policy Review", "Yearly"),
                    new("Incident Report", "Monthly"),
                    new("Legal Compliance", "Quarterly")
                },
                "information technology" => new List<ReportConfig>
                {
                    new("Weekly System Status", "Weekly"),
                    new("Monthly Security", "Monthly"),
                    new("Quarterly Infrastructure", "Quarterly"),
                    new("Annual IT Strategy", "Yearly"),
                    new("Software Licensing", "Quarterly"),
                    new("Backup Report", "Weekly"),
                    new("Network Performance", "Monthly")
                },
                _ => new List<ReportConfig>
                {
                    new("General Report", "Monthly")
                }
            };
        }

        private List<MetricDefinition> GetComprehensiveMetrics(string departmentName, string reportType)
        {
            var baseMetrics = new List<MetricDefinition>
            {
                new("Completion Rate", "Number", "%"),
                new("Quality Score", "Number", "%"),
                new("Processing Time", "Number", "hours"),
                new("User Satisfaction", "Number", "rating"),
                new("Error Rate", "Number", "%"),
                new("Efficiency Rating", "Number", "%")
            };

            var departmentSpecificMetrics = departmentName.ToLower() switch
            {
                "finance" => new List<MetricDefinition>
                {
                    new("Total Revenue", "Currency", "$"),
                    new("Operating Expenses", "Currency", "$"),
                    new("Net Profit", "Currency", "$"),
                    new("Budget Variance", "Number", "%"),
                    new("ROI", "Number", "%"),
                    new("Cash Flow", "Currency", "$"),
                    new("Cost per Transaction", "Currency", "$"),
                    new("Revenue Growth", "Number", "%")
                },
                "human resources" => new List<MetricDefinition>
                {
                    new("Employee Count", "Number", "people"),
                    new("Turnover Rate", "Number", "%"),
                    new("Recruitment Cost", "Currency", "$"),
                    new("Training Hours", "Number", "hours"),
                    new("Employee Satisfaction", "Number", "rating"),
                    new("Absenteeism Rate", "Number", "%"),
                    new("Performance Rating", "Number", "rating"),
                    new("Time to Hire", "Number", "days")
                },
                "operations" => new List<MetricDefinition>
                {
                    new("Production Volume", "Number", "units"),
                    new("Operational Efficiency", "Number", "%"),
                    new("Downtime Hours", "Number", "hours"),
                    new("Quality Defects", "Number", "count"),
                    new("Safety Incidents", "Number", "count"),
                    new("Equipment Utilization", "Number", "%"),
                    new("Cycle Time", "Number", "hours"),
                    new("Maintenance Cost", "Currency", "$")
                },
                "compliance" => new List<MetricDefinition>
                {
                    new("Compliance Score", "Number", "%"),
                    new("Audit Findings", "Number", "count"),
                    new("Risk Level", "Text", "level"),
                    new("Policy Violations", "Number", "count"),
                    new("Training Completion", "Number", "%"),
                    new("Incident Response Time", "Number", "hours"),
                    new("Regulatory Updates", "Number", "count"),
                    new("Documentation Coverage", "Number", "%")
                },
                "information technology" => new List<MetricDefinition>
                {
                    new("System Uptime", "Number", "%"),
                    new("Security Incidents", "Number", "count"),
                    new("Backup Success Rate", "Number", "%"),
                    new("Response Time", "Number", "ms"),
                    new("Storage Utilization", "Number", "%"),
                    new("Network Bandwidth", "Number", "Mbps"),
                    new("Help Desk Tickets", "Number", "count"),
                    new("Software Licenses", "Number", "count")
                },
                _ => new List<MetricDefinition>()
            };

            return baseMetrics.Concat(departmentSpecificMetrics).ToList();
        }

        private object GenerateRealisticMetricValue(MetricDefinition metric, string departmentName, 
            string reportType, DateTime reportDate, string status)
        {
            // Apply seasonal trends and department-specific patterns
            var monthFactor = GetSeasonalFactor(reportDate.Month, metric.Name);
            var departmentFactor = GetDepartmentFactor(departmentName, metric.Name);
            var statusFactor = status == "Approved" ? 1.0 : 0.8; // Approved reports have better metrics

            return metric.Name.ToLower() switch
            {
                // Financial metrics
                "total revenue" => Math.Round(500000 + _random.NextDouble() * 2000000 * monthFactor * departmentFactor),
                "operating expenses" => Math.Round(300000 + _random.NextDouble() * 1000000 * monthFactor),
                "net profit" => Math.Round(50000 + _random.NextDouble() * 500000 * monthFactor * departmentFactor),
                "budget variance" => Math.Round((-10 + _random.NextDouble() * 20) * departmentFactor, 1),
                "roi" => Math.Round((5 + _random.NextDouble() * 25) * departmentFactor * statusFactor, 1),
                "cash flow" => Math.Round(100000 + _random.NextDouble() * 800000 * monthFactor),
                "revenue growth" => Math.Round((-5 + _random.NextDouble() * 25) * monthFactor * departmentFactor, 1),

                // HR metrics
                "employee count" => _random.Next(50, 500),
                "turnover rate" => Math.Round(_random.NextDouble() * 15 * (2 - departmentFactor), 1),
                "recruitment cost" => Math.Round(5000 + _random.NextDouble() * 20000),
                "training hours" => _random.Next(10, 100),
                "employee satisfaction" => Math.Round(3.0 + _random.NextDouble() * 2 * departmentFactor * statusFactor, 1),
                "time to hire" => _random.Next(15, 90),

                // Operations metrics
                "production volume" => _random.Next(1000, 10000),
                "operational efficiency" => Math.Round((60 + _random.NextDouble() * 35) * departmentFactor * statusFactor, 1),
                "downtime hours" => Math.Round(_random.NextDouble() * 24 * (2 - departmentFactor), 1),
                "safety incidents" => _random.Next(0, 5),
                "equipment utilization" => Math.Round((70 + _random.NextDouble() * 25) * departmentFactor, 1),
                "maintenance cost" => Math.Round(10000 + _random.NextDouble() * 50000),

                // Compliance metrics
                "compliance score" => Math.Round((75 + _random.NextDouble() * 25) * departmentFactor * statusFactor, 1),
                "audit findings" => _random.Next(0, 10),
                "risk level" => new[] { "Low", "Medium", "High" }[_random.Next(3)],
                "policy violations" => _random.Next(0, 8),
                "incident response time" => Math.Round(1 + _random.NextDouble() * 24, 1),

                // IT metrics
                "system uptime" => Math.Round((95 + _random.NextDouble() * 5) * statusFactor, 2),
                "security incidents" => _random.Next(0, 5),
                "backup success rate" => Math.Round((90 + _random.NextDouble() * 10) * statusFactor, 1),
                "response time" => Math.Round((10 + _random.NextDouble() * 90) * (2 - departmentFactor)),
                "storage utilization" => Math.Round(40 + _random.NextDouble() * 50, 1),
                "help desk tickets" => _random.Next(50, 500),

                // Base metrics
                "completion rate" => Math.Round((60 + _random.NextDouble() * 35) * departmentFactor * statusFactor, 1),
                "quality score" => Math.Round((70 + _random.NextDouble() * 25) * departmentFactor * statusFactor, 1),
                "processing time" => Math.Round((1 + _random.NextDouble() * 8) * (2 - departmentFactor), 1),
                "user satisfaction" => Math.Round((3.0 + _random.NextDouble() * 2) * departmentFactor * statusFactor, 1),
                "error rate" => Math.Round(_random.NextDouble() * 5 * (2 - departmentFactor), 2),
                "efficiency rating" => Math.Round((65 + _random.NextDouble() * 30) * departmentFactor * statusFactor, 1),

                _ => Math.Round(50 + _random.NextDouble() * 50, 1)
            };
        }

        // Additional helper methods...
        private double GetSeasonalFactor(int month, string metricName)
        {
            // Q4 typically shows higher performance
            if (month >= 10) return 1.1;
            // Q1 typically lower
            if (month <= 3) return 0.9;
            return 1.0;
        }

        private double GetDepartmentFactor(string departmentName, string metricName)
        {
            return departmentName.ToLower() switch
            {
                "finance" => 1.1, // Finance typically performs well
                "compliance" => 0.9, // Compliance can be slower
                "information technology" => 1.05,
                _ => 1.0
            };
        }

        private string FormatValue(object value, string type)
        {
            return type switch
            {
                "Currency" => $"${value:N0}",
                "Number" when value.ToString()!.Contains('.') => $"{value:F1}",
                "Number" => value.ToString()!,
                _ => value.ToString()!
            };
        }

        private decimal? ExtractNumericValue(object value)
        {
            if (decimal.TryParse(value.ToString(), out var result))
                return result;
            return null;
        }

        private DateTime? ExtractDateValue(object value)
        {
            if (DateTime.TryParse(value.ToString(), out var result))
                return result;
            return null;
        }

        private int GetReportsCountForMonth(int monthsBack, string frequency)
        {
            return frequency switch
            {
                "Daily" => monthsBack == 0 ? _random.Next(20, 30) : 0, // Only current month for daily
                "Weekly" => monthsBack <= 1 ? _random.Next(3, 5) : 0, // Only recent months for weekly
                "Monthly" => 1,
                "Quarterly" => monthsBack % 3 == 0 ? 1 : 0,
                "Yearly" => monthsBack == 12 ? 1 : 0,
                _ => monthsBack <= 3 ? 1 : 0
            };
        }

        // ... Continue with more helper methods
        private string GetRealisticsStatus(int monthsBack, string departmentName, ReportConfig config)
        {
            if (monthsBack == 0)
            {
                // Current month - mix of statuses
                var statuses = new[] { "Draft", "Pending", "Approved" };
                var weights = new[] { 30, 40, 30 };
                return GetWeightedRandom(statuses, weights);
            }
            else if (monthsBack <= 2)
            {
                // Recent months - mostly approved
                var statuses = new[] { "Draft", "Pending", "Approved" };
                var weights = new[] { 10, 20, 70 };
                return GetWeightedRandom(statuses, weights);
            }
            else
            {
                // Older months - almost all approved
                var statuses = new[] { "Approved", "Overdue" };
                var weights = new[] { 95, 5 };
                return GetWeightedRandom(statuses, weights);
            }
        }

        private string GetWeightedRandom(string[] options, int[] weights)
        {
            var totalWeight = weights.Sum();
            var randomValue = _random.Next(totalWeight);
            var cumulative = 0;

            for (int i = 0; i < options.Length; i++)
            {
                cumulative += weights[i];
                if (randomValue < cumulative)
                    return options[i];
            }

            return options[0];
        }

        private DateTime GetRandomDateInMonth(DateTime monthStart, DateTime monthEnd)
        {
            var range = (monthEnd - monthStart).Days;
            return monthStart.AddDays(_random.Next(0, range + 1)).AddHours(_random.Next(8, 18));
        }

        private string GenerateReportTitle(string departmentName, string reportType, DateTime date, int index)
        {
            var suffix = index > 0 ? $" #{index + 1}" : "";
            return $"{departmentName} {reportType} Report - {date:MMM yyyy}{suffix}";
        }

        private string GenerateReportDescription(string departmentName, string reportType, DateTime date)
        {
            return $"Comprehensive {reportType.ToLower()} report for {departmentName} department covering the period of {date:MMMM yyyy}. " +
                   $"This report includes detailed analysis, performance metrics, and strategic recommendations.";
        }

        private DateTime GetReportPeriodStart(DateTime createdDate, string reportType)
        {
            return reportType.ToLower() switch
            {
                "daily" => createdDate.Date,
                "weekly" => createdDate.AddDays(-(int)createdDate.DayOfWeek),
                "monthly" => new DateTime(createdDate.Year, createdDate.Month, 1),
                "quarterly" => new DateTime(createdDate.Year, ((createdDate.Month - 1) / 3) * 3 + 1, 1),
                "yearly" => new DateTime(createdDate.Year, 1, 1),
                _ => createdDate.AddDays(-30)
            };
        }

        private DateTime GetReportPeriodEnd(DateTime createdDate, string reportType)
        {
            return reportType.ToLower() switch
            {
                "daily" => createdDate.Date.AddDays(1).AddTicks(-1),
                "weekly" => createdDate.AddDays(6 - (int)createdDate.DayOfWeek),
                "monthly" => new DateTime(createdDate.Year, createdDate.Month, 1).AddMonths(1).AddDays(-1),
                "quarterly" => new DateTime(createdDate.Year, ((createdDate.Month - 1) / 3) * 3 + 1, 1).AddMonths(3).AddDays(-1),
                "yearly" => new DateTime(createdDate.Year, 12, 31),
                _ => createdDate
            };
        }

        private DateTime? GetSubmittedDate(DateTime createdDate, string status)
        {
            if (status == "Draft") return null;
            return createdDate.AddDays(_random.Next(1, 7)).AddHours(_random.Next(1, 24));
        }

        private DateTime? GetApprovedDate(DateTime createdDate, string status)
        {
            if (status != "Approved") return null;
            return createdDate.AddDays(_random.Next(3, 14)).AddHours(_random.Next(1, 24));
        }

        private int? GetApproverUser(string status, List<Users> users)
        {
            if (status != "Approved") return null;
            var executives = users.Where(u => u.Role == "Executive").ToList();
            return executives.Any() ? executives[_random.Next(executives.Count)].Id : null;
        }

        private string GenerateStatusComments(string status, string departmentName, string reportType)
        {
            return status switch
            {
                "Approved" => $"{reportType} report approved after review. Meets {departmentName} department standards.",
                "Pending" => $"Awaiting {departmentName} management approval for {reportType.ToLower()} report.",
                "Overdue" => $"{reportType} report overdue. {departmentName} team requires immediate attention.",
                _ => ""
            };
        }

        private int GetUserLoginFrequency(string role, int? departmentId)
        {
            var baseFrequency = role switch
            {
                "Executive" => _random.Next(40, 80),
                "DepartmentLead" => _random.Next(30, 60),
                "Admin" => _random.Next(50, 90),
                _ => _random.Next(10, 30)
            };

            // Department-specific adjustments
            var departmentFactor = departmentId switch
            {
                1 => 1.2, // Finance - higher activity
                5 => 1.1, // IT - higher activity
                4 => 0.8, // Compliance - lower activity
                _ => 1.0
            };

            return (int)(baseFrequency * departmentFactor);
        }

        private string GenerateRealisticIpAddress()
        {
            var networks = new[] { "192.168.", "10.0.", "172.16." };
            var network = networks[_random.Next(networks.Length)];
            return $"{network}{_random.Next(1, 255)}.{_random.Next(1, 255)}";
        }

        private string GetRealisticUserAgent()
        {
            var userAgents = new[]
            {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/121.0",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Edge/120.0.0.0"
            };
            return userAgents[_random.Next(userAgents.Length)];
        }

        private AuditLog CreateAuditLog(string action, string entityName, int entityId, int userId, 
            string? oldValues, string? newValues, DateTime timestamp)
        {
            return new AuditLog
            {
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                UserId = userId,
                OldValues = oldValues,
                NewValues = newValues,
                Timestamp = timestamp,
                IpAddress = GenerateRealisticIpAddress(),
                UserAgent = GetRealisticUserAgent()
            };
        }

        #endregion

        #region Data Classes

        private record ReportConfig(string Type, string Frequency);
        private record MetricDefinition(string Name, string Type, string Unit);

        #endregion
    }
}
