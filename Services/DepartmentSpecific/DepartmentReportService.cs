using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.Models;
using MultiDeptReportingTool.Services;
using MultiDeptReportingTool.DTOs.DepartmentSpecific;

namespace MultiDeptReportingTool.Services.DepartmentSpecific
{
    /// <summary>
    /// Service for handling department-specific report operations
    /// </summary>
    public class DepartmentReportService : IDepartmentReportService
    {
        private readonly ApplicationDbContext _context;

        public DepartmentReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Finance Department Methods

        public async Task<FinanceReportResponseDto> CreateFinanceReportAsync(CreateFinanceReportDto createDto, string username)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            if (user.Department?.Name != "Finance" && user.Role != "Admin")
                throw new UnauthorizedAccessException("Only Finance department users can create finance reports");

            var report = new Report
            {
                Title = createDto.Title,
                ReportType = createDto.ReportType,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = user.Id,
                DepartmentId = user.DepartmentId ?? 0,
                Comments = createDto.Comments
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            // Create report data entries for finance-specific fields
            var financeDataJson = JsonSerializer.Serialize(createDto.FinanceData);
            var reportData = new ReportData
            {
                ReportId = report.Id,
                FieldName = "FinanceData",
                FieldValue = financeDataJson,
                FieldType = "JSON",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ReportData.Add(reportData);
            await _context.SaveChangesAsync();

            return await GetFinanceReportAsync(report.Id, username) ?? 
                throw new InvalidOperationException("Failed to retrieve created report");
        }

        public async Task<FinanceReportResponseDto?> GetFinanceReportAsync(int reportId, string username)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return null;

            var report = await _context.Reports
                .Include(r => r.CreatedByUser)
                .Include(r => r.Department)
                .Include(r => r.ReportData)
                .Where(r => r.Id == reportId && r.ReportType == "Finance")
                .FirstOrDefaultAsync();

            if (report == null) return null;

            // Check access permissions
            if (!CanAccessReport(report, user))
                return null;

            var financeData = new FinanceReportDto();
            var financeDataItem = report.ReportData.FirstOrDefault(rd => rd.FieldName == "FinanceData");
            if (financeDataItem != null && !string.IsNullOrEmpty(financeDataItem.FieldValue))
            {
                try
                {
                    financeData = JsonSerializer.Deserialize<FinanceReportDto>(financeDataItem.FieldValue) ?? new FinanceReportDto();
                }
                catch
                {
                    financeData = new FinanceReportDto();
                }
            }

            return new FinanceReportResponseDto
            {
                Id = report.Id,
                Title = report.Title,
                Status = report.Status,
                CreatedAt = report.CreatedAt,
                SubmittedAt = report.SubmittedAt,
                ApprovedAt = report.ApprovedAt,
                CreatedBy = report.CreatedByUser.Username,
                DepartmentName = report.Department?.Name ?? "Unknown",
                FinanceData = financeData,
                Comments = report.Comments
            };
        }

        public async Task<List<FinanceReportResponseDto>> GetFinanceReportsAsync(string username, string? status = null, int page = 1, int pageSize = 10)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return new List<FinanceReportResponseDto>();

            var query = _context.Reports
                .Include(r => r.CreatedByUser)
                .Include(r => r.Department)
                .Include(r => r.ReportData)
                .Where(r => r.ReportType == "Finance");

            // Apply access control
            if (user.Role == "Staff")
            {
                query = query.Where(r => r.CreatedByUserId == user.Id || 
                    (r.Status == "Approved" && r.DepartmentId == user.DepartmentId));
            }
            else if (user.Role == "DepartmentLead")
            {
                query = query.Where(r => r.DepartmentId == user.DepartmentId);
            }
            // Admin and Executive can see all

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<FinanceReportResponseDto>();
            foreach (var report in reports)
            {
                var financeReport = await GetFinanceReportAsync(report.Id, username);
                if (financeReport != null)
                    result.Add(financeReport);
            }

            return result;
        }

        public async Task<FinanceReportResponseDto?> UpdateFinanceReportAsync(int reportId, FinanceReportDto financeData, string username)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return null;

            var report = await _context.Reports
                .Include(r => r.ReportData)
                .FirstOrDefaultAsync(r => r.Id == reportId && r.ReportType == "Finance");

            if (report == null) return null;

            // Check edit permissions
            if (!CanEditReport(report, user))
                return null;

            // Update the finance data
            var financeDataJson = JsonSerializer.Serialize(financeData);
            var reportDataItem = report.ReportData.FirstOrDefault(rd => rd.FieldName == "FinanceData");

            if (reportDataItem != null)
            {
                reportDataItem.FieldValue = financeDataJson;
                reportDataItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ReportData.Add(new ReportData
                {
                    ReportId = reportId,
                    FieldName = "FinanceData",
                    FieldValue = financeDataJson,
                    FieldType = "JSON",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return await GetFinanceReportAsync(reportId, username);
        }

        #endregion

        #region HR Department Methods

        public async Task<HRReportResponseDto> CreateHRReportAsync(CreateHRReportDto createDto, string username)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            if (user.Department?.Name != "HR" && user.Role != "Admin")
                throw new UnauthorizedAccessException("Only HR department users can create HR reports");

            var report = new Report
            {
                Title = createDto.Title,
                ReportType = createDto.ReportType,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = user.Id,
                DepartmentId = user.DepartmentId ?? 0,
                Comments = createDto.Comments
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            var hrDataJson = JsonSerializer.Serialize(createDto.HRData);
            var reportData = new ReportData
            {
                ReportId = report.Id,
                FieldName = "HRData",
                FieldValue = hrDataJson,
                FieldType = "JSON",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ReportData.Add(reportData);
            await _context.SaveChangesAsync();

            return await GetHRReportAsync(report.Id, username) ?? 
                throw new InvalidOperationException("Failed to retrieve created report");
        }

        public async Task<HRReportResponseDto?> GetHRReportAsync(int reportId, string username)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return null;

            var report = await _context.Reports
                .Include(r => r.CreatedByUser)
                .Include(r => r.Department)
                .Include(r => r.ReportData)
                .Where(r => r.Id == reportId && r.ReportType == "HR")
                .FirstOrDefaultAsync();

            if (report == null || !CanAccessReport(report, user)) return null;

            var hrData = new HRReportDto();
            var hrDataItem = report.ReportData.FirstOrDefault(rd => rd.FieldName == "HRData");
            if (hrDataItem != null && !string.IsNullOrEmpty(hrDataItem.FieldValue))
            {
                try
                {
                    hrData = JsonSerializer.Deserialize<HRReportDto>(hrDataItem.FieldValue) ?? new HRReportDto();
                }
                catch
                {
                    hrData = new HRReportDto();
                }
            }

            return new HRReportResponseDto
            {
                Id = report.Id,
                Title = report.Title,
                Status = report.Status,
                CreatedAt = report.CreatedAt,
                SubmittedAt = report.SubmittedAt,
                ApprovedAt = report.ApprovedAt,
                CreatedBy = report.CreatedByUser.Username,
                DepartmentName = report.Department?.Name ?? "Unknown",
                HRData = hrData,
                Comments = report.Comments
            };
        }

        public async Task<List<HRReportResponseDto>> GetHRReportsAsync(string username, string? status = null, int page = 1, int pageSize = 10)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return new List<HRReportResponseDto>();

            var query = _context.Reports
                .Include(r => r.CreatedByUser)
                .Include(r => r.Department)
                .Include(r => r.ReportData)
                .Where(r => r.ReportType == "HR");

            query = ApplyAccessControl(query, user);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<HRReportResponseDto>();
            foreach (var report in reports)
            {
                var hrReport = await GetHRReportAsync(report.Id, username);
                if (hrReport != null)
                    result.Add(hrReport);
            }

            return result;
        }

        public async Task<HRReportResponseDto?> UpdateHRReportAsync(int reportId, HRReportDto hrData, string username)
        {
            return await UpdateDepartmentReportDataAsync<HRReportDto, HRReportResponseDto>(
                reportId, hrData, "HRData", "HR", username, GetHRReportAsync);
        }

        #endregion

        #region Operations Department Methods

        public async Task<OperationsReportResponseDto> CreateOperationsReportAsync(CreateOperationsReportDto createDto, string username)
        {
            return await CreateDepartmentReportAsync<CreateOperationsReportDto, OperationsReportResponseDto>(
                createDto, "Operations", username, 
                dto => JsonSerializer.Serialize(dto.OperationsData), 
                GetOperationsReportAsync);
        }

        public async Task<OperationsReportResponseDto?> GetOperationsReportAsync(int reportId, string username)
        {
            return await GetDepartmentReportAsync<OperationsReportDto, OperationsReportResponseDto>(
                reportId, "Operations", "OperationsData", username,
                (report, data) => new OperationsReportResponseDto
                {
                    Id = report.Id,
                    Title = report.Title,
                    Status = report.Status,
                    CreatedAt = report.CreatedAt,
                    SubmittedAt = report.SubmittedAt,
                    ApprovedAt = report.ApprovedAt,
                    CreatedBy = report.CreatedByUser.Username,
                    DepartmentName = report.Department?.Name ?? "Unknown",
                    OperationsData = data,
                    Comments = report.Comments
                });
        }

        public async Task<List<OperationsReportResponseDto>> GetOperationsReportsAsync(string username, string? status = null, int page = 1, int pageSize = 10)
        {
            return await GetDepartmentReportsAsync<OperationsReportResponseDto>(
                "Operations", username, status, page, pageSize, GetOperationsReportAsync);
        }

        public async Task<OperationsReportResponseDto?> UpdateOperationsReportAsync(int reportId, OperationsReportDto operationsData, string username)
        {
            return await UpdateDepartmentReportDataAsync<OperationsReportDto, OperationsReportResponseDto>(
                reportId, operationsData, "OperationsData", "Operations", username, GetOperationsReportAsync);
        }

        #endregion

        #region Compliance Department Methods

        public async Task<ComplianceReportResponseDto> CreateComplianceReportAsync(CreateComplianceReportDto createDto, string username)
        {
            return await CreateDepartmentReportAsync<CreateComplianceReportDto, ComplianceReportResponseDto>(
                createDto, "Compliance", username, 
                dto => JsonSerializer.Serialize(dto.ComplianceData), 
                GetComplianceReportAsync);
        }

        public async Task<ComplianceReportResponseDto?> GetComplianceReportAsync(int reportId, string username)
        {
            return await GetDepartmentReportAsync<ComplianceReportDto, ComplianceReportResponseDto>(
                reportId, "Compliance", "ComplianceData", username,
                (report, data) => new ComplianceReportResponseDto
                {
                    Id = report.Id,
                    Title = report.Title,
                    Status = report.Status,
                    CreatedAt = report.CreatedAt,
                    SubmittedAt = report.SubmittedAt,
                    ApprovedAt = report.ApprovedAt,
                    CreatedBy = report.CreatedByUser.Username,
                    DepartmentName = report.Department?.Name ?? "Unknown",
                    ComplianceData = data,
                    Comments = report.Comments
                });
        }

        public async Task<List<ComplianceReportResponseDto>> GetComplianceReportsAsync(string username, string? status = null, int page = 1, int pageSize = 10)
        {
            return await GetDepartmentReportsAsync<ComplianceReportResponseDto>(
                "Compliance", username, status, page, pageSize, GetComplianceReportAsync);
        }

        public async Task<ComplianceReportResponseDto?> UpdateComplianceReportAsync(int reportId, ComplianceReportDto complianceData, string username)
        {
            return await UpdateDepartmentReportDataAsync<ComplianceReportDto, ComplianceReportResponseDto>(
                reportId, complianceData, "ComplianceData", "Compliance", username, GetComplianceReportAsync);
        }

        #endregion

        #region IT Department Methods

        public async Task<ITReportResponseDto> CreateITReportAsync(CreateITReportDto createDto, string username)
        {
            return await CreateDepartmentReportAsync<CreateITReportDto, ITReportResponseDto>(
                createDto, "IT", username, 
                dto => JsonSerializer.Serialize(dto.ITData), 
                GetITReportAsync);
        }

        public async Task<ITReportResponseDto?> GetITReportAsync(int reportId, string username)
        {
            return await GetDepartmentReportAsync<ITReportDto, ITReportResponseDto>(
                reportId, "IT", "ITData", username,
                (report, data) => new ITReportResponseDto
                {
                    Id = report.Id,
                    Title = report.Title,
                    Status = report.Status,
                    CreatedAt = report.CreatedAt,
                    SubmittedAt = report.SubmittedAt,
                    ApprovedAt = report.ApprovedAt,
                    CreatedBy = report.CreatedByUser.Username,
                    DepartmentName = report.Department?.Name ?? "Unknown",
                    ITData = data,
                    Comments = report.Comments
                });
        }

        public async Task<List<ITReportResponseDto>> GetITReportsAsync(string username, string? status = null, int page = 1, int pageSize = 10)
        {
            return await GetDepartmentReportsAsync<ITReportResponseDto>(
                "IT", username, status, page, pageSize, GetITReportAsync);
        }

        public async Task<ITReportResponseDto?> UpdateITReportAsync(int reportId, ITReportDto itData, string username)
        {
            return await UpdateDepartmentReportDataAsync<ITReportDto, ITReportResponseDto>(
                reportId, itData, "ITData", "IT", username, GetITReportAsync);
        }

        #endregion

        #region Generic Methods

        public async Task<bool> SubmitDepartmentReportAsync(int reportId, string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return false;

            var report = await _context.Reports.FindAsync(reportId);
            if (report == null || report.Status != "Draft") return false;

            if (report.CreatedByUserId != user.Id && user.Role != "Admin")
                return false;

            report.Status = "Pending";
            report.SubmittedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ApproveDepartmentReportAsync(int reportId, string username, string? comments = null)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return false;

            var report = await _context.Reports
                .Include(r => r.Department)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null || report.Status != "Pending") return false;

            // Check approval permissions
            if (!CanApproveReport(report, user)) return false;

            report.Status = "Approved";
            report.ApprovedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(comments))
                report.Comments = comments;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectDepartmentReportAsync(int reportId, string username, string comments)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return false;

            var report = await _context.Reports
                .Include(r => r.Department)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null || report.Status != "Pending") return false;

            if (!CanApproveReport(report, user)) return false;

            report.Status = "Draft";
            report.Comments = comments;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteDepartmentReportAsync(int reportId, string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return false;

            var report = await _context.Reports
                .Include(r => r.ReportData)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null) return false;

            if (report.Status != "Draft" && user.Role != "Admin")
                return false;

            if (report.CreatedByUserId != user.Id && user.Role != "Admin")
                return false;

            _context.ReportData.RemoveRange(report.ReportData);
            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Analytics and Dashboard Methods

        public async Task<object> GetDepartmentAnalyticsAsync(string departmentName, string username, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name == departmentName);

            if (department == null)
                return new { error = "Department not found" };

            var query = _context.Reports
                .Where(r => r.DepartmentId == department.Id);

            if (fromDate.HasValue)
                query = query.Where(r => r.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(r => r.CreatedAt <= toDate.Value);

            var reports = await query.ToListAsync();

            return new
            {
                Department = departmentName,
                TotalReports = reports.Count,
                DraftReports = reports.Count(r => r.Status == "Draft"),
                PendingReports = reports.Count(r => r.Status == "Pending"),
                ApprovedReports = reports.Count(r => r.Status == "Approved"),
                ReportsByType = reports.GroupBy(r => r.ReportType)
                    .Select(g => new { Type = g.Key, Count = g.Count() }),
                MonthlyTrends = reports.GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                    .Select(g => new { 
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}", 
                        Count = g.Count() 
                    }),
                AnalysisPeriod = new
                {
                    From = fromDate?.ToString("yyyy-MM-dd") ?? "All Time",
                    To = toDate?.ToString("yyyy-MM-dd") ?? "Present"
                }
            };
        }

        public async Task<object> GetDepartmentDashboardAsync(string departmentName, string username)
        {
            var analytics = await GetDepartmentAnalyticsAsync(departmentName, username, DateTime.UtcNow.AddDays(-30));
            
            var recentReports = await _context.Reports
                .Include(r => r.CreatedByUser)
                .Include(r => r.Department)
                .Where(r => r.Department!.Name == departmentName)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.Status,
                    r.CreatedAt,
                    CreatedBy = r.CreatedByUser.Username
                })
                .ToListAsync();

            return new
            {
                Analytics = analytics,
                RecentReports = recentReports,
                QuickActions = new[]
                {
                    "Create New Report",
                    "View Pending Approvals",
                    "Generate Analytics",
                    "Export Reports"
                }
            };
        }

        public async Task<List<object>> GetCrossDepartmentMetricsAsync(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || (user.Role != "Admin" && user.Role != "Executive"))
                throw new UnauthorizedAccessException("Access denied");

            var departments = await _context.Departments.ToListAsync();
            var metrics = new List<object>();

            foreach (var dept in departments)
            {
                var deptMetrics = await GetDepartmentAnalyticsAsync(dept.Name, username);
                metrics.Add(deptMetrics);
            }

            return metrics;
        }

        #endregion

        #region Helper Methods

        private static bool CanAccessReport(Report report, Users user)
        {
            return user.Role switch
            {
                "Admin" or "Executive" => true,
                "DepartmentLead" => report.DepartmentId == user.DepartmentId,
                "Staff" => report.CreatedByUserId == user.Id || 
                          (report.Status == "Approved" && report.DepartmentId == user.DepartmentId),
                _ => false
            };
        }

        private static bool CanEditReport(Report report, Users user)
        {
            if (user.Role == "Admin") return true;
            if (report.Status != "Draft") return false;
            
            return user.Role switch
            {
                "DepartmentLead" => report.DepartmentId == user.DepartmentId,
                "Staff" => report.CreatedByUserId == user.Id,
                _ => false
            };
        }

        private static bool CanApproveReport(Report report, Users user)
        {
            return user.Role switch
            {
                "Admin" or "Executive" => true,
                "DepartmentLead" => report.DepartmentId == user.DepartmentId,
                _ => false
            };
        }

        private static IQueryable<Report> ApplyAccessControl(IQueryable<Report> query, Users user)
        {
            return user.Role switch
            {
                "Admin" or "Executive" => query,
                "DepartmentLead" => query.Where(r => r.DepartmentId == user.DepartmentId),
                "Staff" => query.Where(r => r.CreatedByUserId == user.Id || 
                    (r.Status == "Approved" && r.DepartmentId == user.DepartmentId)),
                _ => query.Where(r => false)
            };
        }

        private async Task<TResponse> CreateDepartmentReportAsync<TCreate, TResponse>(
            TCreate createDto, 
            string departmentName, 
            string username,
            Func<TCreate, string> serializeData,
            Func<int, string, Task<TResponse?>> getReport)
            where TCreate : class
            where TResponse : class
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            if (user.Department?.Name != departmentName && user.Role != "Admin")
                throw new UnauthorizedAccessException($"Only {departmentName} department users can create {departmentName} reports");

            // Get title and comments using reflection
            var titleProperty = typeof(TCreate).GetProperty("Title");
            var commentsProperty = typeof(TCreate).GetProperty("Comments");
            
            var title = titleProperty?.GetValue(createDto) as string ?? $"{departmentName} Report";
            var comments = commentsProperty?.GetValue(createDto) as string;

            var report = new Report
            {
                Title = title,
                ReportType = departmentName,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = user.Id,
                DepartmentId = user.DepartmentId ?? 0,
                Comments = comments
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            var dataJson = serializeData(createDto);
            var reportData = new ReportData
            {
                ReportId = report.Id,
                FieldName = $"{departmentName}Data",
                FieldValue = dataJson,
                FieldType = "JSON",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ReportData.Add(reportData);
            await _context.SaveChangesAsync();

            return await getReport(report.Id, username) ?? 
                throw new InvalidOperationException("Failed to retrieve created report");
        }

        private async Task<TResponse?> GetDepartmentReportAsync<TData, TResponse>(
            int reportId, 
            string reportType, 
            string dataFieldName, 
            string username,
            Func<Report, TData, TResponse> createResponse)
            where TData : class, new()
            where TResponse : class
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return null;

            var report = await _context.Reports
                .Include(r => r.CreatedByUser)
                .Include(r => r.Department)
                .Include(r => r.ReportData)
                .Where(r => r.Id == reportId && r.ReportType == reportType)
                .FirstOrDefaultAsync();

            if (report == null || !CanAccessReport(report, user)) return null;

            var data = new TData();
            var dataItem = report.ReportData.FirstOrDefault(rd => rd.FieldName == dataFieldName);
            if (dataItem != null && !string.IsNullOrEmpty(dataItem.FieldValue))
            {
                try
                {
                    data = JsonSerializer.Deserialize<TData>(dataItem.FieldValue) ?? new TData();
                }
                catch
                {
                    data = new TData();
                }
            }

            return createResponse(report, data);
        }

        private async Task<List<TResponse>> GetDepartmentReportsAsync<TResponse>(
            string reportType, 
            string username, 
            string? status, 
            int page, 
            int pageSize,
            Func<int, string, Task<TResponse?>> getReport)
            where TResponse : class
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return new List<TResponse>();

            var query = _context.Reports
                .Where(r => r.ReportType == reportType);

            query = ApplyAccessControl(query, user);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var reportIds = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => r.Id)
                .ToListAsync();

            var result = new List<TResponse>();
            foreach (var reportId in reportIds)
            {
                var report = await getReport(reportId, username);
                if (report != null)
                    result.Add(report);
            }

            return result;
        }

        private async Task<TResponse?> UpdateDepartmentReportDataAsync<TData, TResponse>(
            int reportId, 
            TData data, 
            string dataFieldName, 
            string reportType, 
            string username,
            Func<int, string, Task<TResponse?>> getReport)
            where TData : class
            where TResponse : class
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return null;

            var report = await _context.Reports
                .Include(r => r.ReportData)
                .FirstOrDefaultAsync(r => r.Id == reportId && r.ReportType == reportType);

            if (report == null || !CanEditReport(report, user)) return null;

            var dataJson = JsonSerializer.Serialize(data);
            var reportDataItem = report.ReportData.FirstOrDefault(rd => rd.FieldName == dataFieldName);

            if (reportDataItem != null)
            {
                reportDataItem.FieldValue = dataJson;
                reportDataItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ReportData.Add(new ReportData
                {
                    ReportId = reportId,
                    FieldName = dataFieldName,
                    FieldValue = dataJson,
                    FieldType = "JSON",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return await getReport(reportId, username);
        }

        #endregion
    }
}
