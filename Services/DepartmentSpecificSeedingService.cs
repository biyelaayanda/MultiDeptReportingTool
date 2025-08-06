using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.Models;
using System.Text.Json;

namespace MultiDeptReportingTool.Services
{
    public class DepartmentSpecificSeedingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DepartmentSpecificSeedingService> _logger;

        public DepartmentSpecificSeedingService(ApplicationDbContext context, ILogger<DepartmentSpecificSeedingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedDepartmentSpecificDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting department-specific data seeding...");

                // Clear existing data first
                await ClearExistingDataAsync();

                // Get departments
                var departments = await _context.Departments.ToListAsync();
                var users = await _context.Users.ToListAsync();

                if (!departments.Any() || !users.Any())
                {
                    _logger.LogWarning("No departments or users found. Cannot seed department-specific data.");
                    return;
                }

                // Seed reports for each department
                await SeedDepartmentReportsAsync(departments, users);
                await SeedDepartmentReportDataAsync();
                await SeedDepartmentAuditLogsAsync(users);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Department-specific data seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding department-specific data");
                throw;
            }
        }

        private async Task ClearExistingDataAsync()
        {
            _logger.LogInformation("Clearing existing department data...");
            
            _context.AuditLogs.RemoveRange(_context.AuditLogs);
            _context.ReportData.RemoveRange(_context.ReportData);
            _context.Reports.RemoveRange(_context.Reports);
            
            await _context.SaveChangesAsync();
        }

        private async Task SeedDepartmentReportsAsync(List<Department> departments, List<Users> users)
        {
            _logger.LogInformation("Seeding department-specific reports...");

            var random = new Random(42);
            var now = DateTime.UtcNow;
            var reports = new List<Report>();

            foreach (var department in departments)
            {
                var departmentUsers = users.Where(u => u.DepartmentId == department.Id).ToList();
                if (!departmentUsers.Any())
                {
                    // Use any user as fallback
                    departmentUsers = users.Take(1).ToList();
                }

                var reportTypes = GetDepartmentReportTypes(department.Name);
                var departmentReportCount = GetDepartmentReportCount(department.Name);

                // Generate reports for the last 6 months
                for (int monthsBack = 6; monthsBack >= 0; monthsBack--)
                {
                    var periodStart = now.AddMonths(-monthsBack).AddDays(-now.Day + 1);
                    var periodEnd = periodStart.AddMonths(1).AddDays(-1);

                    var reportsThisMonth = random.Next(departmentReportCount.min, departmentReportCount.max + 1);

                    for (int i = 0; i < reportsThisMonth; i++)
                    {
                        var createdByUser = departmentUsers[random.Next(departmentUsers.Count)];
                        var reportType = reportTypes[random.Next(reportTypes.Length)];
                        var status = GetReportStatusForDepartment(monthsBack, department.Name, random);

                        var report = new Report
                        {
                            Title = $"{department.Name} {reportType} Report - {periodStart:MMM yyyy}",
                            Description = GetDepartmentReportDescription(department.Name, reportType, periodStart, periodEnd),
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
                            Comments = GetStatusComment(status, department.Name)
                        };

                        reports.Add(report);
                    }
                }
            }

            await _context.Reports.AddRangeAsync(reports);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Seeded {reports.Count} department-specific reports");
        }

        private async Task SeedDepartmentReportDataAsync()
        {
            _logger.LogInformation("Seeding department-specific report data...");

            var reports = await _context.Reports.Include(r => r.Department).ToListAsync();
            var random = new Random(42);
            var reportDataList = new List<ReportData>();

            foreach (var report in reports)
            {
                var metrics = GetDepartmentSpecificMetrics(report.Department!.Name);
                
                foreach (var metric in metrics)
                {
                    var value = GenerateDepartmentSpecificValue(metric, report.Department.Name, report, random);
                    
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
            _logger.LogInformation($"Seeded {reportDataList.Count} department-specific report data entries");
        }

        private async Task SeedDepartmentAuditLogsAsync(List<Users> users)
        {
            _logger.LogInformation("Seeding department-specific audit logs...");

            var reports = await _context.Reports.ToListAsync();
            var random = new Random(42);
            var auditLogs = new List<AuditLog>();

            // Generate audit logs for each department's activities
            foreach (var user in users)
            {
                var userReports = reports.Where(r => r.CreatedByUserId == user.Id).ToList();
                
                // Login activities - varied by department
                var loginCount = GetDepartmentLoginFrequency(user.DepartmentId, random);
                for (int i = 0; i < loginCount; i++)
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

                // Report activities
                foreach (var report in userReports)
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
            }

            await _context.AuditLogs.AddRangeAsync(auditLogs);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Seeded {auditLogs.Count} department-specific audit log entries");
        }

        // Helper methods for department-specific data
        private string[] GetDepartmentReportTypes(string departmentName)
        {
            return departmentName.ToLower() switch
            {
                "finance" => new[] { "Financial", "Budget", "Revenue", "Cost Analysis", "Investment", "Tax", "Audit" },
                "hr" => new[] { "Performance", "Recruitment", "Training", "Employee", "Compensation", "Benefits", "Compliance" },
                "operations" => new[] { "Operational", "Production", "Quality", "Efficiency", "Process", "Safety", "Maintenance" },
                "compliance" => new[] { "Compliance", "Risk", "Audit", "Regulatory", "Policy", "Legal", "Security" },
                "it" => new[] { "Technical", "Security", "Infrastructure", "Software", "Network", "Data", "System" },
                _ => new[] { "General", "Monthly", "Quarterly" }
            };
        }

        private (int min, int max) GetDepartmentReportCount(string departmentName)
        {
            return departmentName.ToLower() switch
            {
                "finance" => (3, 6), // Finance has more reports
                "hr" => (2, 4),
                "operations" => (3, 5),
                "compliance" => (2, 3), // Compliance has fewer reports
                "it" => (2, 4),
                _ => (1, 3)
            };
        }

        private string GetReportStatusForDepartment(int monthsBack, string departmentName, Random random)
        {
            // Department-specific status patterns
            var baseStatuses = new[] { "Draft", "Pending", "Approved", "Overdue" };
            var weights = departmentName.ToLower() switch
            {
                "finance" => monthsBack <= 1 ? new[] { 10, 30, 50, 10 } : new[] { 5, 15, 75, 5 }, // Finance is more efficient
                "hr" => monthsBack <= 1 ? new[] { 20, 40, 30, 10 } : new[] { 10, 20, 65, 5 },
                "operations" => monthsBack <= 1 ? new[] { 15, 35, 40, 10 } : new[] { 5, 25, 65, 5 },
                "compliance" => monthsBack <= 1 ? new[] { 25, 35, 30, 10 } : new[] { 15, 25, 55, 5 }, // Compliance can be slower
                "it" => monthsBack <= 1 ? new[] { 20, 30, 40, 10 } : new[] { 10, 20, 65, 5 },
                _ => monthsBack <= 1 ? new[] { 25, 25, 25, 25 } : new[] { 10, 20, 60, 10 }
            };

            return GetWeightedRandom(baseStatuses, weights, random);
        }

        private string GetDepartmentReportDescription(string departmentName, string reportType, DateTime start, DateTime end)
        {
            return departmentName.ToLower() switch
            {
                "finance" => $"Comprehensive {reportType.ToLower()} analysis for Finance department covering fiscal period {start:MMM dd} to {end:MMM dd, yyyy}. Includes revenue tracking, expense analysis, and budget variance reporting.",
                "hr" => $"Human Resources {reportType.ToLower()} report for period {start:MMM dd} to {end:MMM dd, yyyy}. Covers employee metrics, performance evaluations, and workforce analytics.",
                "operations" => $"Operations {reportType.ToLower()} assessment covering {start:MMM dd} to {end:MMM dd, yyyy}. Includes production metrics, efficiency analysis, and process optimization recommendations.",
                "compliance" => $"Compliance {reportType.ToLower()} report for regulatory period {start:MMM dd} to {end:MMM dd, yyyy}. Covers regulatory adherence, risk assessment, and audit findings.",
                "it" => $"Information Technology {reportType.ToLower()} report covering {start:MMM dd} to {end:MMM dd, yyyy}. Includes system performance, security assessments, and infrastructure analysis.",
                _ => $"{departmentName} {reportType.ToLower()} report for {start:MMM dd} to {end:MMM dd, yyyy}"
            };
        }

        private string GetStatusComment(string status, string departmentName)
        {
            return status switch
            {
                "Approved" => $"Report approved by {departmentName} management after thorough review",
                "Pending" => $"Awaiting {departmentName} department head approval",
                "Overdue" => $"Report overdue - {departmentName} team requires follow-up",
                _ => null
            } ?? string.Empty;
        }

        private List<string> GetDepartmentSpecificMetrics(string departmentName)
        {
            var baseMetrics = new List<string>
            {
                "CompletionRate", "ResponseTime", "QualityScore", "EfficiencyRating", 
                "UserSatisfaction", "ProcessingTime", "ErrorRate", "ActiveUsers"
            };

            var departmentMetrics = departmentName.ToLower() switch
            {
                "finance" => new[] { "RevenueTrend", "CostReduction", "BudgetVariance", "ROI", "ProfitMargin", "CashFlow", "ExpenseRatio" },
                "hr" => new[] { "EmployeeRetention", "RecruitmentTime", "TrainingHours", "PerformanceScore", "TurnoverRate", "EmployeeSatisfaction", "AbsenteeismRate" },
                "operations" => new[] { "ProductivityIndex", "DowntimeHours", "ThroughputRate", "MaintenanceCost", "QualityDefects", "CycleTime", "CapacityUtilization" },
                "compliance" => new[] { "ComplianceScore", "AuditFindings", "RiskLevel", "DocumentationRate", "IncidentCount", "TrainingCompletion", "PolicyAdherence" },
                "it" => new[] { "SystemUptime", "SecurityIncidents", "BackupSuccess", "NetworkLatency", "ServiceAvailability", "TicketResolution", "SystemLoad" },
                _ => new[] { "GeneralMetric1", "GeneralMetric2" }
            };

            return baseMetrics.Concat(departmentMetrics).ToList();
        }

        private object GenerateDepartmentSpecificValue(string metric, string departmentName, Report report, Random random)
        {
            // Generate realistic values based on department and metric
            return (departmentName.ToLower(), metric.ToLower()) switch
            {
                ("finance", "revenuetrend") => Math.Round(-5 + random.NextDouble() * 25, 1), // -5% to +20%
                ("finance", "costreduction") => Math.Round(random.NextDouble() * 15, 1), // 0-15%
                ("finance", "budgetvariance") => Math.Round(-10 + random.NextDouble() * 20, 1), // -10% to +10%
                ("finance", "roi") => Math.Round(5 + random.NextDouble() * 25, 1), // 5-30%
                ("finance", "profitmargin") => Math.Round(10 + random.NextDouble() * 20, 1), // 10-30%
                ("finance", "cashflow") => random.Next(50000, 500000), // $50K-$500K

                ("hr", "employeeretention") => Math.Round(80 + random.NextDouble() * 15, 1), // 80-95%
                ("hr", "recruitmenttime") => random.Next(15, 60), // 15-60 days
                ("hr", "traininghours") => random.Next(20, 80), // 20-80 hours
                ("hr", "performancescore") => Math.Round(3.0 + random.NextDouble() * 2, 1), // 3.0-5.0
                ("hr", "turnoverrate") => Math.Round(random.NextDouble() * 15, 1), // 0-15%
                ("hr", "employeesatisfaction") => Math.Round(3.5 + random.NextDouble() * 1.5, 1), // 3.5-5.0

                ("operations", "productivityindex") => Math.Round(75 + random.NextDouble() * 25, 1), // 75-100%
                ("operations", "downtimehours") => Math.Round(random.NextDouble() * 12, 1), // 0-12 hours
                ("operations", "throughputrate") => Math.Round(70 + random.NextDouble() * 30, 1), // 70-100%
                ("operations", "maintenancecost") => random.Next(5000, 50000), // $5K-$50K
                ("operations", "qualitydefects") => random.Next(0, 10), // 0-10 defects
                ("operations", "cycletime") => Math.Round(1 + random.NextDouble() * 8, 1), // 1-9 hours

                ("compliance", "compliancescore") => Math.Round(80 + random.NextDouble() * 20, 1), // 80-100%
                ("compliance", "auditfindings") => random.Next(0, 8), // 0-8 findings
                ("compliance", "risklevel") => new[] { "Low", "Medium", "High" }[random.Next(3)],
                ("compliance", "documentationrate") => Math.Round(70 + random.NextDouble() * 30, 1), // 70-100%
                ("compliance", "incidentcount") => random.Next(0, 5), // 0-5 incidents
                ("compliance", "trainingcompletion") => Math.Round(75 + random.NextDouble() * 25, 1), // 75-100%

                ("it", "systemuptime") => Math.Round(95 + random.NextDouble() * 5, 2), // 95-100%
                ("it", "securityincidents") => random.Next(0, 5), // 0-5 incidents
                ("it", "backupsuccess") => Math.Round(90 + random.NextDouble() * 10, 1), // 90-100%
                ("it", "networklatency") => Math.Round(5 + random.NextDouble() * 45, 0), // 5-50ms
                ("it", "serviceavailability") => Math.Round(98 + random.NextDouble() * 2, 2), // 98-100%
                ("it", "ticketresolution") => Math.Round(0.5 + random.NextDouble() * 3.5, 1), // 0.5-4 hours

                // Base metrics
                ("finance", "completionrate") => Math.Round(75 + random.NextDouble() * 20, 1), // Finance is efficient
                ("hr", "completionrate") => Math.Round(65 + random.NextDouble() * 25, 1),
                ("operations", "completionrate") => Math.Round(70 + random.NextDouble() * 25, 1),
                ("compliance", "completionrate") => Math.Round(60 + random.NextDouble() * 30, 1), // Compliance can be slower
                ("it", "completionrate") => Math.Round(68 + random.NextDouble() * 27, 1),

                _ when metric.Contains("rate") || metric.Contains("score") => Math.Round(50 + random.NextDouble() * 45, 1),
                _ when metric.Contains("time") || metric.Contains("hours") => Math.Round(1 + random.NextDouble() * 8, 1),
                _ when metric.Contains("count") || metric.Contains("incidents") => random.Next(0, 10),
                _ => Math.Round(50 + random.NextDouble() * 50, 1)
            };
        }

        private int GetDepartmentLoginFrequency(int? departmentId, Random random)
        {
            return departmentId switch
            {
                1 => random.Next(40, 80), // Finance - high activity
                2 => random.Next(25, 50), // HR - medium activity  
                3 => random.Next(30, 60), // Operations - medium-high activity
                4 => random.Next(20, 40), // Compliance - lower activity
                5 => random.Next(35, 70), // IT - high activity
                _ => random.Next(15, 30)
            };
        }

        private string GetFieldType(string metric)
        {
            return metric.ToLower() switch
            {
                var m when m.Contains("level") || m.Contains("satisfaction") => "Text",
                var m when m.Contains("count") || m.Contains("findings") || m.Contains("incidents") => "Number",
                var m when m.Contains("date") || m.Contains("time") => "Date",
                _ => "Number"
            };
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

        private string GenerateRandomIpAddress(Random random)
        {
            return $"192.168.{random.Next(1, 255)}.{random.Next(1, 255)}";
        }
    }
}
