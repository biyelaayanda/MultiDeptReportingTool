using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Models;

namespace MultiDeptReportingTool.Services
{
    public interface IMfaService
    {
        /// <summary>
        /// Generate MFA setup information including QR code and backup codes
        /// </summary>
        Task<MfaSetupResponseDto> GenerateMfaSetupAsync(int userId, string? ipAddress = null);

        /// <summary>
        /// Enable MFA for a user after verifying the setup code
        /// </summary>
        Task<bool> EnableMfaAsync(int userId, string verificationCode);

        /// <summary>
        /// Disable MFA for a user
        /// </summary>
        Task<bool> DisableMfaAsync(int userId, string currentPassword, string mfaCode);

        /// <summary>
        /// Verify TOTP code for authentication
        /// </summary>
        Task<bool> VerifyTotpCodeAsync(int userId, string code);

        /// <summary>
        /// Verify backup code and mark as used
        /// </summary>
        Task<bool> VerifyBackupCodeAsync(int userId, string backupCode);

        /// <summary>
        /// Generate new backup codes (invalidates old ones)
        /// </summary>
        Task<List<string>> GenerateNewBackupCodesAsync(int userId);

        /// <summary>
        /// Get MFA status for a user
        /// </summary>
        Task<MfaStatusDto> GetMfaStatusAsync(int userId);

        /// <summary>
        /// Check if MFA is required for this user
        /// </summary>
        Task<bool> IsMfaRequiredAsync(int userId);

        /// <summary>
        /// Record failed MFA attempt and handle lockout
        /// </summary>
        Task RecordFailedMfaAttemptAsync(int userId);

        /// <summary>
        /// Check if user is locked out due to failed MFA attempts
        /// </summary>
        Task<bool> IsUserMfaLockedAsync(int userId);

        /// <summary>
        /// Reset MFA lockout for a user
        /// </summary>
        Task ResetMfaLockoutAsync(int userId);

        /// <summary>
        /// Get QR code for manual MFA setup
        /// </summary>
        Task<string> GenerateQrCodeAsync(string secret, string userEmail, string issuer = "MultiDeptReportingTool");
    }
}
