using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Models;

namespace MultiDeptReportingTool.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReportResponseDto?> CreateReportAsync(CreateReportDto createReportDto, int createdByUserId)
        {
            try
            {
                // Validate department exists
                var department = await _context.Departments.FindAsync(createReportDto.DepartmentId);
                if (department == null)
                {
                    return null;
                }

                var report = new Report
                {
                    Title = createReportDto.Title,
                    Description = createReportDto.Description,
                    ReportType = createReportDto.ReportType,
                    Status = "Draft",
                    DepartmentId = createReportDto.DepartmentId,
                    CreatedByUserId = createdByUserId,
                    ReportPeriodStart = createReportDto.ReportPeriodStart,
                    ReportPeriodEnd = createReportDto.ReportPeriodEnd,
                    Comments = createReportDto.Comments,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                // Add report data if provided
                if (createReportDto.ReportData != null && createReportDto.ReportData.Any())
                {
                    var reportDataEntities = createReportDto.ReportData.Select(rd => new ReportData
                    {
                        ReportId = report.Id,
                        FieldName = rd.FieldName,
                        FieldType = rd.FieldType,
                        FieldValue = rd.FieldValue,
                        NumericValue = rd.NumericValue,
                        DateValue = rd.DateValue,
                        CreatedAt = DateTime.UtcNow
                    }).ToList();

                    _context.ReportData.AddRange(reportDataEntities);
                    await _context.SaveChangesAsync();
                }

                return await GetReportByIdAsync(report.Id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<ReportResponseDto?> GetReportByIdAsync(int reportId)
        {
            var report = await _context.Reports
                .Include(r => r.Department)
                .Include(r => r.CreatedByUser)
                .Include(r => r.ApprovedByUser)
                .Include(r => r.ReportData)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
            {
                return null;
            }

            return new ReportResponseDto
            {
                Id = report.Id,
                Title = report.Title,
                Description = report.Description,
                ReportType = report.ReportType,
                Status = report.Status,
                DepartmentId = report.DepartmentId,
                DepartmentName = report.Department?.Name ?? "",
                CreatedByUserId = report.CreatedByUserId,
                CreatedByUserName = $"{report.CreatedByUser?.FirstName} {report.CreatedByUser?.LastName}".Trim(),
                ReportPeriodStart = report.ReportPeriodStart,
                ReportPeriodEnd = report.ReportPeriodEnd,
                CreatedAt = report.CreatedAt,
                SubmittedAt = report.SubmittedAt,
                ApprovedAt = report.ApprovedAt,
                ApprovedByUserId = report.ApprovedByUserId,
                ApprovedByUserName = report.ApprovedByUser != null ? $"{report.ApprovedByUser.FirstName} {report.ApprovedByUser.LastName}".Trim() : null,
                Comments = report.Comments,
                ReportData = report.ReportData.Select(rd => new ReportDataResponseDto
                {
                    Id = rd.Id,
                    FieldName = rd.FieldName,
                    FieldType = rd.FieldType,
                    FieldValue = rd.FieldValue,
                    NumericValue = rd.NumericValue,
                    DateValue = rd.DateValue,
                    CreatedAt = rd.CreatedAt,
                    UpdatedAt = rd.UpdatedAt
                }).ToList()
            };
        }

        public async Task<(List<ReportResponseDto> Reports, int TotalCount)> GetReportsAsync(ReportFilterDto filter)
        {
            var query = _context.Reports
                .Include(r => r.Department)
                .Include(r => r.CreatedByUser)
                .Include(r => r.ApprovedByUser)
                .AsQueryable();

            // Apply filters
            if (filter.DepartmentId.HasValue)
            {
                query = query.Where(r => r.DepartmentId == filter.DepartmentId.Value);
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                query = query.Where(r => r.Status == filter.Status);
            }

            if (!string.IsNullOrEmpty(filter.ReportType))
            {
                query = query.Where(r => r.ReportType == filter.ReportType);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= filter.EndDate.Value);
            }

            if (filter.CreatedByUserId.HasValue)
            {
                query = query.Where(r => r.CreatedByUserId == filter.CreatedByUserId.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "title" => filter.SortDirection.ToLower() == "asc" ? query.OrderBy(r => r.Title) : query.OrderByDescending(r => r.Title),
                "status" => filter.SortDirection.ToLower() == "asc" ? query.OrderBy(r => r.Status) : query.OrderByDescending(r => r.Status),
                "reporttype" => filter.SortDirection.ToLower() == "asc" ? query.OrderBy(r => r.ReportType) : query.OrderByDescending(r => r.ReportType),
                "department" => filter.SortDirection.ToLower() == "asc" ? query.OrderBy(r => r.Department!.Name) : query.OrderByDescending(r => r.Department!.Name),
                _ => filter.SortDirection.ToLower() == "asc" ? query.OrderBy(r => r.CreatedAt) : query.OrderByDescending(r => r.CreatedAt)
            };

            // Apply pagination
            var reports = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(r => new ReportResponseDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    Description = r.Description,
                    ReportType = r.ReportType,
                    Status = r.Status,
                    DepartmentId = r.DepartmentId,
                    DepartmentName = r.Department!.Name,
                    CreatedByUserId = r.CreatedByUserId,
                    CreatedByUserName = $"{r.CreatedByUser!.FirstName} {r.CreatedByUser.LastName}".Trim(),
                    ReportPeriodStart = r.ReportPeriodStart,
                    ReportPeriodEnd = r.ReportPeriodEnd,
                    CreatedAt = r.CreatedAt,
                    SubmittedAt = r.SubmittedAt,
                    ApprovedAt = r.ApprovedAt,
                    ApprovedByUserId = r.ApprovedByUserId,
                    ApprovedByUserName = r.ApprovedByUser != null ? $"{r.ApprovedByUser.FirstName} {r.ApprovedByUser.LastName}".Trim() : null,
                    Comments = r.Comments,
                    ReportData = new List<ReportDataResponseDto>() // Don't load full data for list view
                })
                .ToListAsync();

            return (reports, totalCount);
        }

        public async Task<ReportResponseDto?> UpdateReportAsync(int reportId, UpdateReportDto updateReportDto, int userId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
            {
                return null;
            }

            // Check if user can edit (only if in Draft status and user is creator or admin)
            if (!await CanUserEditReportAsync(reportId, userId, "", null))
            {
                return null;
            }

            report.Title = updateReportDto.Title;
            report.Description = updateReportDto.Description;
            report.ReportType = updateReportDto.ReportType;
            report.ReportPeriodStart = updateReportDto.ReportPeriodStart;
            report.ReportPeriodEnd = updateReportDto.ReportPeriodEnd;
            report.Comments = updateReportDto.Comments;

            await _context.SaveChangesAsync();

            // Update report data if provided
            if (updateReportDto.ReportData != null)
            {
                await UpdateReportDataAsync(reportId, updateReportDto.ReportData, userId);
            }

            return await GetReportByIdAsync(reportId);
        }

        public async Task<bool> DeleteReportAsync(int reportId, int userId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
            {
                return false;
            }

            // Only allow deletion if in Draft status and user is creator or admin
            if (report.Status != "Draft" || !await CanUserEditReportAsync(reportId, userId, "", null))
            {
                return false;
            }

            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ReportResponseDto?> SubmitReportAsync(int reportId, ReportSubmissionDto submissionDto, int userId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null || report.Status != "Draft")
            {
                return null;
            }

            // Check if user can submit (creator or department lead)
            if (!await CanUserEditReportAsync(reportId, userId, "", null))
            {
                return null;
            }

            report.Status = "Pending";
            report.SubmittedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(submissionDto.Comments))
            {
                report.Comments = submissionDto.Comments;
            }

            await _context.SaveChangesAsync();
            return await GetReportByIdAsync(reportId);
        }

        public async Task<ReportResponseDto?> ApproveReportAsync(int reportId, ReportApprovalDto approvalDto, int userId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null || report.Status != "Pending")
            {
                return null;
            }

            report.Status = "Approved";
            report.ApprovedAt = DateTime.UtcNow;
            report.ApprovedByUserId = userId;
            if (!string.IsNullOrEmpty(approvalDto.Comments))
            {
                report.Comments = approvalDto.Comments;
            }

            await _context.SaveChangesAsync();
            return await GetReportByIdAsync(reportId);
        }

        public async Task<ReportResponseDto?> RejectReportAsync(int reportId, ReportApprovalDto rejectionDto, int userId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null || report.Status != "Pending")
            {
                return null;
            }

            report.Status = "Draft"; // Send back to draft for revision
            report.Comments = rejectionDto.Comments;

            await _context.SaveChangesAsync();
            return await GetReportByIdAsync(reportId);
        }

        public async Task<List<ReportDataResponseDto>> GetReportDataAsync(int reportId)
        {
            var reportData = await _context.ReportData
                .Where(rd => rd.ReportId == reportId)
                .OrderBy(rd => rd.FieldName)
                .ToListAsync();

            return reportData.Select(rd => new ReportDataResponseDto
            {
                Id = rd.Id,
                FieldName = rd.FieldName,
                FieldType = rd.FieldType,
                FieldValue = rd.FieldValue,
                NumericValue = rd.NumericValue,
                DateValue = rd.DateValue,
                CreatedAt = rd.CreatedAt,
                UpdatedAt = rd.UpdatedAt
            }).ToList();
        }

        public async Task<bool> UpdateReportDataAsync(int reportId, List<UpdateReportDataDto> reportData, int userId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null || !await CanUserEditReportAsync(reportId, userId, "", null))
            {
                return false;
            }

            // Get existing report data
            var existingData = await _context.ReportData
                .Where(rd => rd.ReportId == reportId)
                .ToListAsync();

            // Update existing items and add new ones
            foreach (var item in reportData)
            {
                if (item.Id.HasValue)
                {
                    // Update existing
                    var existing = existingData.FirstOrDefault(ed => ed.Id == item.Id.Value);
                    if (existing != null)
                    {
                        existing.FieldName = item.FieldName;
                        existing.FieldType = item.FieldType;
                        existing.FieldValue = item.FieldValue;
                        existing.NumericValue = item.NumericValue;
                        existing.DateValue = item.DateValue;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    // Add new
                    var newReportData = new ReportData
                    {
                        ReportId = reportId,
                        FieldName = item.FieldName,
                        FieldType = item.FieldType,
                        FieldValue = item.FieldValue,
                        NumericValue = item.NumericValue,
                        DateValue = item.DateValue,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ReportData.Add(newReportData);
                }
            }

            // Remove data items not in the update list
            var updatedIds = reportData.Where(rd => rd.Id.HasValue).Select(rd => rd.Id.Value).ToList();
            var toRemove = existingData.Where(ed => !updatedIds.Contains(ed.Id)).ToList();
            _context.ReportData.RemoveRange(toRemove);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanUserAccessReportAsync(int reportId, int userId, string userRole, int? userDepartmentId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
            {
                return false;
            }

            // Admin and Executive can access all reports
            if (userRole == "Admin" || userRole == "Executive")
            {
                return true;
            }

            // Department leads can access reports in their department
            if (userRole == "DepartmentLead" && userDepartmentId == report.DepartmentId)
            {
                return true;
            }

            // Users can access their own reports
            if (report.CreatedByUserId == userId)
            {
                return true;
            }

            // Staff in same department can view approved reports
            if (userDepartmentId == report.DepartmentId && report.Status == "Approved")
            {
                return true;
            }

            return false;
        }

        public async Task<bool> CanUserEditReportAsync(int reportId, int userId, string userRole, int? userDepartmentId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
            {
                return false;
            }

            // Only drafts can be edited (except by admin)
            if (report.Status != "Draft" && userRole != "Admin")
            {
                return false;
            }

            // Admin can edit any report
            if (userRole == "Admin")
            {
                return true;
            }

            // Users can edit their own draft reports
            if (report.CreatedByUserId == userId && report.Status == "Draft")
            {
                return true;
            }

            // Department leads can edit drafts in their department
            if (userRole == "DepartmentLead" && userDepartmentId == report.DepartmentId && report.Status == "Draft")
            {
                return true;
            }

            return false;
        }
    }
}
