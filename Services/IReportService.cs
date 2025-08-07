using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Models;

namespace MultiDeptReportingTool.Services
{
    public interface IReportService
    {
        // CRUD Operations
        Task<ReportResponseDto?> CreateReportAsync(CreateReportDto createReportDto, int createdByUserId);
        Task<ReportResponseDto?> GetReportByIdAsync(int reportId);
        Task<(List<ReportResponseDto> Reports, int TotalCount)> GetReportsAsync(ReportFilterDto filter);
        Task<(List<ReportResponseDto> Reports, int TotalCount)> GetReportsAsync(ReportFilterDto filter, int userId, string userRole, int? userDepartmentId);
        Task<ReportResponseDto?> UpdateReportAsync(int reportId, UpdateReportDto updateReportDto, int userId);
        Task<bool> DeleteReportAsync(int reportId, int userId);

        // Workflow Operations
        Task<ReportResponseDto?> SubmitReportAsync(int reportId, ReportSubmissionDto submissionDto, int userId);
        Task<ReportResponseDto?> ApproveReportAsync(int reportId, ReportApprovalDto approvalDto, int userId);
        Task<ReportResponseDto?> RejectReportAsync(int reportId, ReportApprovalDto rejectionDto, int userId);

        // Data Operations
        Task<List<ReportDataResponseDto>> GetReportDataAsync(int reportId);
        Task<bool> UpdateReportDataAsync(int reportId, List<UpdateReportDataDto> reportData, int userId);

        // User Access Control
        Task<bool> CanUserAccessReportAsync(int reportId, int userId, string userRole, int? userDepartmentId);
        Task<bool> CanUserEditReportAsync(int reportId, int userId, string userRole, int? userDepartmentId);
    }
}
