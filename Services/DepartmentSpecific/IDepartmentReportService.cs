using MultiDeptReportingTool.DTOs.DepartmentSpecific;

namespace MultiDeptReportingTool.Services.DepartmentSpecific
{
    /// <summary>
    /// Interface for department-specific report services
    /// </summary>
    public interface IDepartmentReportService
    {
        // Finance Department Methods
        Task<FinanceReportResponseDto> CreateFinanceReportAsync(CreateFinanceReportDto createDto, string username);
        Task<FinanceReportResponseDto?> GetFinanceReportAsync(int reportId, string username);
        Task<List<FinanceReportResponseDto>> GetFinanceReportsAsync(string username, string? status = null, int page = 1, int pageSize = 10);
        Task<FinanceReportResponseDto?> UpdateFinanceReportAsync(int reportId, FinanceReportDto financeData, string username);
        
        // HR Department Methods
        Task<HRReportResponseDto> CreateHRReportAsync(CreateHRReportDto createDto, string username);
        Task<HRReportResponseDto?> GetHRReportAsync(int reportId, string username);
        Task<List<HRReportResponseDto>> GetHRReportsAsync(string username, string? status = null, int page = 1, int pageSize = 10);
        Task<HRReportResponseDto?> UpdateHRReportAsync(int reportId, HRReportDto hrData, string username);
        
        // Operations Department Methods
        Task<OperationsReportResponseDto> CreateOperationsReportAsync(CreateOperationsReportDto createDto, string username);
        Task<OperationsReportResponseDto?> GetOperationsReportAsync(int reportId, string username);
        Task<List<OperationsReportResponseDto>> GetOperationsReportsAsync(string username, string? status = null, int page = 1, int pageSize = 10);
        Task<OperationsReportResponseDto?> UpdateOperationsReportAsync(int reportId, OperationsReportDto operationsData, string username);
        
        // Compliance Department Methods
        Task<ComplianceReportResponseDto> CreateComplianceReportAsync(CreateComplianceReportDto createDto, string username);
        Task<ComplianceReportResponseDto?> GetComplianceReportAsync(int reportId, string username);
        Task<List<ComplianceReportResponseDto>> GetComplianceReportsAsync(string username, string? status = null, int page = 1, int pageSize = 10);
        Task<ComplianceReportResponseDto?> UpdateComplianceReportAsync(int reportId, ComplianceReportDto complianceData, string username);
        
        // IT Department Methods
        Task<ITReportResponseDto> CreateITReportAsync(CreateITReportDto createDto, string username);
        Task<ITReportResponseDto?> GetITReportAsync(int reportId, string username);
        Task<List<ITReportResponseDto>> GetITReportsAsync(string username, string? status = null, int page = 1, int pageSize = 10);
        Task<ITReportResponseDto?> UpdateITReportAsync(int reportId, ITReportDto itData, string username);
        
        // Generic Methods for All Department Reports
        Task<bool> SubmitDepartmentReportAsync(int reportId, string username);
        Task<bool> ApproveDepartmentReportAsync(int reportId, string username, string? comments = null);
        Task<bool> RejectDepartmentReportAsync(int reportId, string username, string comments);
        Task<bool> DeleteDepartmentReportAsync(int reportId, string username);
        
        // Analytics and Dashboard Methods
        Task<object> GetDepartmentAnalyticsAsync(string departmentName, string username, DateTime? fromDate = null, DateTime? toDate = null);
        Task<object> GetDepartmentDashboardAsync(string departmentName, string username);
        Task<List<object>> GetCrossDepartmentMetricsAsync(string username);
    }
}
