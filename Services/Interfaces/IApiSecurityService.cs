using System.Threading.Tasks;

namespace MultiDeptReportingTool.Services.Interfaces
{
    public interface IApiSecurityService
    {
        // Request signing and validation
        Task<string> SignRequestAsync(string requestBody, string timestamp, string method, string path);
        Task<bool> ValidateRequestSignatureAsync(string signature, string requestBody, string timestamp, string method, string path);
        
        // Request validation
        Task<bool> ValidateRequestAsync(HttpRequest request);
        Task<bool> IsRequestThrottledAsync(string clientId, string endpoint);
        
        // API versioning support
        string GetRequestVersion(HttpRequest request);
        bool IsVersionSupported(string version);
        
        // Rate limiting per endpoint
        Task<bool> CheckEndpointRateLimitAsync(string endpoint, string clientId);
        Task RecordApiCallAsync(string endpoint, string clientId, bool successful);
        
        // Request sanitization
        Task<string> SanitizeInputAsync(string input, string inputType);
        Task<bool> IsInputSafeAsync(string input, string inputType);
    }
}
