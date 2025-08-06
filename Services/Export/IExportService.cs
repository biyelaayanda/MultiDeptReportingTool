using MultiDeptReportingTool.DTOs.Export;
using MultiDeptReportingTool.DTOs.Analytics;

namespace MultiDeptReportingTool.Services.Export
{
    /// <summary>
    /// Export service interface for generating reports and handling email notifications
    /// </summary>
    public interface IExportService
    {
        // PDF Export Methods
        Task<ExportResponseDto> ExportToPdfAsync(ExportRequestDto request);
        Task<byte[]> GeneratePdfReportAsync(object reportData, string templateName = "default");
        Task<byte[]> GenerateDashboardPdfAsync(ExecutiveDashboardDto dashboard);

        // Excel Export Methods
        Task<ExportResponseDto> ExportToExcelAsync(ExportRequestDto request);
        Task<byte[]> GenerateExcelReportAsync(object reportData, string worksheetName = "Report");
        Task<byte[]> GenerateMultiSheetExcelAsync(Dictionary<string, object> worksheetData);

        // CSV Export Methods
        Task<ExportResponseDto> ExportToCsvAsync(ExportRequestDto request);
        Task<byte[]> GenerateCsvReportAsync(IEnumerable<object> data, string[] headers = null);

        // JSON Export Methods
        Task<ExportResponseDto> ExportToJsonAsync(ExportRequestDto request);
        Task<string> GenerateJsonReportAsync(object reportData);

        // PowerPoint Export Methods
        Task<ExportResponseDto> ExportToPowerPointAsync(ExportRequestDto request);
        Task<byte[]> GeneratePowerPointReportAsync(object reportData, string templateName = "default");

        // Chart Generation Methods
        Task<byte[]> GenerateChartImageAsync(ChartConfigDto chartConfig);
        Task<List<byte[]>> GenerateMultipleChartsAsync(List<ChartConfigDto> chartConfigs);
        Task<byte[]> GenerateDashboardChartsAsync(ExecutiveDashboardDto dashboard);

        // Email Methods
        Task<bool> SendEmailAsync(EmailNotificationDto emailDto);
        Task<bool> SendEmailWithAttachmentAsync(string toEmail, string subject, string body, ExportResponseDto attachment);
        Task<bool> SendBulkEmailAsync(List<EmailNotificationDto> emails);
        Task<bool> SendScheduledReportAsync(ScheduledReportDto scheduledReport);

        // Template Methods
        Task<string> GetEmailTemplateAsync(string templateName, Dictionary<string, object> parameters);
        Task<string> GetReportTemplateAsync(string templateName, Dictionary<string, object> parameters);
        Task<bool> SaveCustomTemplateAsync(string templateName, string templateContent);

        // Scheduled Reports Methods
        Task<List<ScheduledReportDto>> GetScheduledReportsAsync(string userId = null);
        Task<ScheduledReportDto> CreateScheduledReportAsync(ScheduledReportDto scheduledReport);
        Task<bool> UpdateScheduledReportAsync(int reportId, ScheduledReportDto scheduledReport);
        Task<bool> DeleteScheduledReportAsync(int reportId);
        Task<bool> RunScheduledReportAsync(int reportId);
        Task<List<ScheduledReportDto>> GetDueScheduledReportsAsync();

        // File Management Methods
        Task<string> SaveExportFileAsync(byte[] fileData, string fileName, string contentType);
        Task<byte[]> GetExportFileAsync(string filePath);
        Task<bool> DeleteExportFileAsync(string filePath);
        Task<List<string>> GetExportHistoryAsync(string userId, int limit = 50);

        // Validation Methods
        bool ValidateExportRequest(ExportRequestDto request, out List<string> errors);
        bool ValidateEmailRequest(EmailNotificationDto emailDto, out List<string> errors);
        bool ValidateChartConfig(ChartConfigDto chartConfig, out List<string> errors);
    }
}
